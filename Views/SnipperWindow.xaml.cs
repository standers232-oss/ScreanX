using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreanX.Models;
using ScreanX.Services;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace ScreanX.Views
{
    public partial class SnipperWindow : Window
    {
        private readonly Bitmap _fullDesktopBmp;
        private readonly Rectangle _screenBounds;
        private readonly BitmapSource _fullDesktopSource;

        private bool _isSelecting = false;
        private bool _isAnnotating = false;
        private Point _startPoint;
        private Rect _selectionRect = Rect.Empty;

        private AnnotationType _currentTool = AnnotationType.Pen;
        private Color _currentColor = Colors.Red;
        private double _currentStroke = 4;
        private int _stepCounter = 1;

        private readonly List<UIElement> _annotationElements = new();
        private UIElement? _currentDraftElement = null;
        private List<Point> _currentPenPoints = new();

        public Action<Bitmap>? OnCaptureComplete { get; set; }
        public Action<Rectangle>? OnStartRecord { get; set; }

        public SnipperWindow(Bitmap fullDesktop)
        {
            InitializeComponent();
            _fullDesktopBmp = fullDesktop;
            _screenBounds = CaptureService.GetVirtualScreenBounds();

            Left = _screenBounds.Left;
            Top = _screenBounds.Top;
            Width = _screenBounds.Width;
            Height = _screenBounds.Height;

            _fullDesktopSource = CaptureService.ToBitmapSource(_fullDesktopBmp);
            BackgroundImage.Source = _fullDesktopSource;
            BackgroundImage.Width = _screenBounds.Width;
            BackgroundImage.Height = _screenBounds.Height;

            UpdateDarkOverlay();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
            LoupeContainer.Visibility = Visibility.Visible;
        }

        private void UpdateDarkOverlay()
        {
            var fullRect = new RectangleGeometry(new Rect(0, 0, Width, Height));
            if (_selectionRect.IsEmpty || _selectionRect.Width <= 0 || _selectionRect.Height <= 0)
            {
                DarkOverlayPath.Data = fullRect;
            }
            else
            {
                var selectRect = new RectangleGeometry(_selectionRect, 4, 4);
                var combined = new CombinedGeometry(GeometryCombineMode.Exclude, fullRect, selectRect);
                DarkOverlayPath.Data = combined;
            }
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            Point currentPoint = e.GetPosition(RootGrid);

            // If we don't have a selection yet, start selecting region
            if (_selectionRect.IsEmpty || !_selectionRect.Contains(currentPoint))
            {
                _isSelecting = true;
                _isAnnotating = false;
                _startPoint = currentPoint;
                _selectionRect = new Rect(_startPoint, new System.Windows.Size(0, 0));

                SelectionBorder.Visibility = Visibility.Visible;
                DimensionBadge.Visibility = Visibility.Visible;
                FloatingToolbar.Visibility = Visibility.Collapsed;
                AnnotationCanvas.Visibility = Visibility.Collapsed;
                LoupeContainer.Visibility = Visibility.Collapsed;

                UpdateSelectionView();
                return;
            }

            // If we are inside selected region and have an annotation tool active
            if (!_selectionRect.IsEmpty && _selectionRect.Contains(currentPoint) && ToolSelect.IsChecked != true)
            {
                _isAnnotating = true;
                _startPoint = currentPoint;

                switch (_currentTool)
                {
                    case AnnotationType.StepBadge:
                        PlaceStepBadge(currentPoint);
                        _isAnnotating = false;
                        break;

                    case AnnotationType.Text:
                        PlaceTextBox(currentPoint);
                        _isAnnotating = false;
                        break;

                    case AnnotationType.Pen:
                    case AnnotationType.Highlighter:
                        _currentPenPoints = new List<Point> { currentPoint };
                        var polyline = new Polyline
                        {
                            Stroke = new SolidColorBrush(_currentColor),
                            StrokeThickness = _currentTool == AnnotationType.Highlighter ? _currentStroke * 2.5 : _currentStroke,
                            Opacity = _currentTool == AnnotationType.Highlighter ? 0.45 : 1.0,
                            StrokeLineJoin = PenLineJoin.Round,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round
                        };
                        polyline.Points.Add(currentPoint);
                        _currentDraftElement = polyline;
                        AnnotationCanvas.Children.Add(polyline);
                        break;

                    case AnnotationType.Arrow:
                    case AnnotationType.Rectangle:
                    case AnnotationType.Ellipse:
                    case AnnotationType.Blur:
                        // Draft shape will be updated in MouseMove
                        break;
                }
            }
        }

        private void RootGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(RootGrid);

            // Update Loupe if visible
            if (LoupeContainer.Visibility == Visibility.Visible)
            {
                UpdateLoupe(currentPoint);
            }

            // Region selection dragging
            if (_isSelecting)
            {
                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(currentPoint.X - _startPoint.X);
                double h = Math.Abs(currentPoint.Y - _startPoint.Y);

                _selectionRect = new Rect(x, y, w, h);
                UpdateSelectionView();
                return;
            }

            // Annotation drawing inside selection
            if (_isAnnotating)
            {
                UpdateAnnotationDraft(currentPoint);
            }
        }

        private void RootGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_isSelecting)
            {
                _isSelecting = false;
                if (_selectionRect.Width < 10 || _selectionRect.Height < 10)
                {
                    // Selection too small, reset
                    _selectionRect = Rect.Empty;
                    SelectionBorder.Visibility = Visibility.Collapsed;
                    DimensionBadge.Visibility = Visibility.Collapsed;
                    FloatingToolbar.Visibility = Visibility.Collapsed;
                    AnnotationCanvas.Visibility = Visibility.Collapsed;
                    LoupeContainer.Visibility = Visibility.Visible;
                    UpdateDarkOverlay();
                    return;
                }

                // Selection confirmed, position Toolbar
                PositionToolbar();
                AnnotationCanvas.Visibility = Visibility.Visible;
                FloatingToolbar.Visibility = Visibility.Visible;
                return;
            }

            if (_isAnnotating)
            {
                _isAnnotating = false;
                if (_currentDraftElement != null)
                {
                    _annotationElements.Add(_currentDraftElement);
                    _currentDraftElement = null;
                }
            }
        }

        private void UpdateSelectionView()
        {
            Canvas.SetLeft(SelectionBorder, _selectionRect.X);
            Canvas.SetTop(SelectionBorder, _selectionRect.Y);
            SelectionBorder.Width = Math.Max(0, _selectionRect.Width);
            SelectionBorder.Height = Math.Max(0, _selectionRect.Height);

            bool isSmall = SettingsService.Current.AutoEnhanceSmallSnippets && 
                           ImageEnhanceService.ShouldEnhance((int)_selectionRect.Width, (int)_selectionRect.Height, SettingsService.Current.SmallSnippetThreshold);

            if (isSmall && HDEnhanceToggle.IsChecked != true)
            {
                HDEnhanceToggle.IsChecked = true;
            }

            UpdateDimensionLabel();
            Canvas.SetLeft(DimensionBadge, _selectionRect.X);
            Canvas.SetTop(DimensionBadge, Math.Max(10, _selectionRect.Y - 32));

            UpdateDarkOverlay();
        }

        private void UpdateDimensionLabel()
        {
            string hdTag = HDEnhanceToggle.IsChecked == true ? " • ⚡ 2x HD" : "";
            DimensionText.Text = $"{(int)_selectionRect.Width} × {(int)_selectionRect.Height} px{hdTag}";
        }

        private void HDEnhanceToggle_Click(object sender, RoutedEventArgs e)
        {
            UpdateDimensionLabel();
        }

        private void PositionToolbar()
        {
            double tbWidth = 650;
            double tbHeight = 48;

            double tbX = _selectionRect.X + (_selectionRect.Width - tbWidth) / 2.0;
            tbX = Math.Max(10, Math.Min(Width - tbWidth - 10, tbX));

            // Position below selection, or above if close to bottom
            double tbY = _selectionRect.Y + _selectionRect.Height + 12;
            if (tbY + tbHeight > Height - 10)
            {
                tbY = _selectionRect.Y - tbHeight - 12;
            }
            if (tbY < 10)
            {
                tbY = _selectionRect.Y + 12;
            }

            Canvas.SetLeft(FloatingToolbar, tbX);
            Canvas.SetTop(FloatingToolbar, tbY);

            if (FloatingToolbar.Visibility != Visibility.Visible)
            {
                FloatingToolbar.Visibility = Visibility.Visible;
                FloatingToolbar.Opacity = 0;
                ToolbarScale.ScaleX = 0.88;
                ToolbarScale.ScaleY = 0.88;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
                var pop = new DoubleAnimation(0.88, 1.0, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
                };

                FloatingToolbar.BeginAnimation(OpacityProperty, fadeIn);
                ToolbarScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
                ToolbarScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            }
        }

        private void UpdateLoupe(Point pt)
        {
            double loupeX = pt.X + 25;
            double loupeY = pt.Y + 25;
            if (loupeX + 150 > Width) loupeX = pt.X - 165;
            if (loupeY + 150 > Height) loupeY = pt.Y - 165;

            Canvas.SetLeft(LoupeContainer, loupeX);
            Canvas.SetTop(LoupeContainer, loupeY);

            int px = (int)Math.Clamp(pt.X, 0, _fullDesktopBmp.Width - 1);
            int py = (int)Math.Clamp(pt.Y, 0, _fullDesktopBmp.Height - 1);

            System.Drawing.Color color = _fullDesktopBmp.GetPixel(px, py);
            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            LoupeHexText.Text = $"{hex} ({color.R},{color.G},{color.B})";
            LoupeColorSwatch.Background = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));

            // Create 18x18 cropped loupe bitmap
            int cropSize = 18;
            int cx = Math.Clamp(px - cropSize / 2, 0, _fullDesktopBmp.Width - cropSize);
            int cy = Math.Clamp(py - cropSize / 2, 0, _fullDesktopBmp.Height - cropSize);
            using var crop = CaptureService.CropBitmap(_fullDesktopBmp, new Rectangle(cx, cy, cropSize, cropSize));
            LoupeImage.Source = CaptureService.ToBitmapSource(crop);
        }

        private void UpdateAnnotationDraft(Point currentPoint)
        {
            if (_currentTool == AnnotationType.Pen || _currentTool == AnnotationType.Highlighter)
            {
                if (_currentDraftElement is Polyline poly)
                {
                    _currentPenPoints.Add(currentPoint);
                    poly.Points.Add(currentPoint);
                }
                return;
            }

            // Remove previous draft if any
            if (_currentDraftElement != null)
            {
                AnnotationCanvas.Children.Remove(_currentDraftElement);
                _currentDraftElement = null;
            }

            double rx = Math.Min(_startPoint.X, currentPoint.X);
            double ry = Math.Min(_startPoint.Y, currentPoint.Y);
            double rw = Math.Max(1, Math.Abs(currentPoint.X - _startPoint.X));
            double rh = Math.Max(1, Math.Abs(currentPoint.Y - _startPoint.Y));

            switch (_currentTool)
            {
                case AnnotationType.Arrow:
                    _currentDraftElement = CreateArrowElement(_startPoint, currentPoint, _currentColor, _currentStroke);
                    break;

                case AnnotationType.Rectangle:
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(_currentColor),
                        StrokeThickness = _currentStroke,
                        RadiusX = 4,
                        RadiusY = 4,
                        Width = rw,
                        Height = rh
                    };
                    Canvas.SetLeft(rect, rx);
                    Canvas.SetTop(rect, ry);
                    _currentDraftElement = rect;
                    break;

                case AnnotationType.Ellipse:
                    var ellipse = new System.Windows.Shapes.Ellipse
                    {
                        Stroke = new SolidColorBrush(_currentColor),
                        StrokeThickness = _currentStroke,
                        Width = rw,
                        Height = rh
                    };
                    Canvas.SetLeft(ellipse, rx);
                    Canvas.SetTop(ellipse, ry);
                    _currentDraftElement = ellipse;
                    break;

                case AnnotationType.Blur:
                    // Pixelated obfuscation box
                    _currentDraftElement = CreateBlurElement(new Rect(rx, ry, rw, rh));
                    break;
            }

            if (_currentDraftElement != null)
            {
                AnnotationCanvas.Children.Add(_currentDraftElement);
            }
        }

        private UIElement CreateArrowElement(Point start, Point end, Color color, double thickness)
        {
            var grid = new Canvas();
            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            grid.Children.Add(line);

            // Arrow head
            double theta = Math.Atan2(end.Y - start.Y, end.X - start.X);
            double headLength = 14 + thickness * 2;
            double arrowAngle = Math.PI / 6.0;

            Point p1 = new Point(
                end.X - headLength * Math.Cos(theta - arrowAngle),
                end.Y - headLength * Math.Sin(theta - arrowAngle)
            );
            Point p2 = new Point(
                end.X - headLength * Math.Cos(theta + arrowAngle),
                end.Y - headLength * Math.Sin(theta + arrowAngle)
            );

            var head = new Polygon
            {
                Fill = new SolidColorBrush(color),
                Points = new PointCollection { end, p1, p2 }
            };
            grid.Children.Add(head);
            return grid;
        }

        private UIElement CreateBlurElement(Rect rect)
        {
            int rx = (int)rect.X;
            int ry = (int)rect.Y;
            int rw = (int)rect.Width;
            int rh = (int)rect.Height;

            if (rw < 4 || rh < 4) return new Canvas();

            // Pixelate effect: downsample to 10x smaller then scale up
            using var cropped = CaptureService.CropBitmap(_fullDesktopBmp, new Rectangle(rx, ry, rw, rh));
            int pixelBlock = 8;
            int smallW = Math.Max(1, rw / pixelBlock);
            int smallH = Math.Max(1, rh / pixelBlock);

            using var small = new Bitmap(smallW, smallH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(cropped, 0, 0, smallW, smallH);
            }

            using var pixelated = new Bitmap(rw, rh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(pixelated))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(small, 0, 0, rw, rh);
            }

            var image = new System.Windows.Controls.Image
            {
                Source = CaptureService.ToBitmapSource(pixelated),
                Width = rw,
                Height = rh
            };
            Canvas.SetLeft(image, rx);
            Canvas.SetTop(image, ry);
            return image;
        }

        private void PlaceStepBadge(Point point)
        {
            var badge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(_currentColor),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.5 }
            };

            var text = new TextBlock
            {
                Text = _stepCounter++.ToString(),
                Foreground = Colors.White == _currentColor ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            badge.Child = text;

            Canvas.SetLeft(badge, point.X - 14);
            Canvas.SetTop(badge, point.Y - 14);
            AnnotationCanvas.Children.Add(badge);
            _annotationElements.Add(badge);
        }

        private void PlaceTextBox(Point point)
        {
            var textBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Foreground = new SolidColorBrush(_currentColor),
                CaretBrush = new SolidColorBrush(_currentColor),
                BorderBrush = new SolidColorBrush(_currentColor),
                BorderThickness = new Thickness(1),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(6, 4, 6, 4),
                MinWidth = 80
            };

            Canvas.SetLeft(textBox, point.X);
            Canvas.SetTop(textBox, point.Y);
            AnnotationCanvas.Children.Add(textBox);
            _annotationElements.Add(textBox);

            textBox.Loaded += (s, e) => textBox.Focus();
        }

        private Bitmap RenderFinalSelectionBitmap()
        {
            int rx = (int)_selectionRect.X;
            int ry = (int)_selectionRect.Y;
            int rw = (int)_selectionRect.Width;
            int rh = (int)_selectionRect.Height;

            // 1. Crop background image
            var resultBmp = CaptureService.CropBitmap(_fullDesktopBmp, new Rectangle(rx, ry, rw, rh));

            // 2. If there are annotations, render them over the cropped bitmap
            if (_annotationElements.Count > 0)
            {
                var rtb = new RenderTargetBitmap(rw, rh, 96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    // Draw cropped background
                    dc.DrawImage(CaptureService.ToBitmapSource(resultBmp), new Rect(0, 0, rw, rh));

                    // Shift annotations coordinate to (0, 0) of selection
                    dc.PushTransform(new TranslateTransform(-rx, -ry));
                    var vb = new VisualBrush(AnnotationCanvas);
                    dc.DrawRectangle(vb, null, new Rect(rx, ry, rw, rh));
                    dc.Pop();
                }
                rtb.Render(visual);

                // Convert RenderTargetBitmap to System.Drawing.Bitmap
                using var ms = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                resultBmp.Dispose();
                resultBmp = new Bitmap(ms);
            }

            // 3. Super-Sampling Enhancement for Small Snippets
            if (HDEnhanceToggle.IsChecked == true)
            {
                var enhanced = ImageEnhanceService.EnhanceSmallImage(resultBmp, 2.0);
                resultBmp.Dispose();
                return enhanced;
            }

            return resultBmp;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            FinishCapture(copyClipboard: true, saveFile: SettingsService.Current.AutoSaveFile);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            FinishCapture(copyClipboard: true, saveFile: true);
        }

        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionRect.Width < 10 || _selectionRect.Height < 10) return;
            var finalBmp = RenderFinalSelectionBitmap();
            var pinned = new PinnedWindow(finalBmp, _selectionRect.X + _screenBounds.Left, _selectionRect.Y + _screenBounds.Top);
            pinned.Show();
            Close();
        }

        private void FinishCapture(bool copyClipboard, bool saveFile)
        {
            if (_selectionRect.Width < 10 || _selectionRect.Height < 10)
            {
                Close();
                return;
            }

            var finalBmp = RenderFinalSelectionBitmap();
            var settings = SettingsService.Current;

            if (copyClipboard)
            {
                try
                {
                    var source = CaptureService.ToBitmapSource(finalBmp);
                    Clipboard.SetImage(source);
                }
                catch { }
            }

            if (saveFile)
            {
                try
                {
                    string dir = settings.SaveDirectory;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string fileName = $"ScreanX_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{settings.ImageFormat.ToLowerInvariant()}";
                    string filePath = System.IO.Path.Combine(dir, fileName);

                    ImageFormat format = settings.ImageFormat.ToUpperInvariant() switch
                    {
                        "JPEG" or "JPG" => ImageFormat.Jpeg,
                        "BMP" => ImageFormat.Bmp,
                        _ => ImageFormat.Png
                    };

                    finalBmp.Save(filePath, format);
                    HistoryService.AddItem(filePath, finalBmp.Width, finalBmp.Height);
                }
                catch { }
            }

            SoundService.PlayCaptureSound();
            OnCaptureComplete?.Invoke(finalBmp);
            Close();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_annotationElements.Count > 0)
            {
                var last = _annotationElements[^1];
                _annotationElements.RemoveAt(_annotationElements.Count - 1);
                AnnotationCanvas.Children.Remove(last);
            }
        }

        private void RecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionRect.Width < 50 || _selectionRect.Height < 50)
            {
                ToastNotification.ShowToast("Слишком маленькая область для записи");
                return;
            }

            int x = (int)Math.Round(_selectionRect.X + _screenBounds.Left);
            int y = (int)Math.Round(_selectionRect.Y + _screenBounds.Top);
            int w = (int)Math.Round(_selectionRect.Width);
            int h = (int)Math.Round(_selectionRect.Height);

            var area = new Rectangle(x, y, w, h);
            var recordCallback = OnStartRecord;
            Close();
            recordCallback?.Invoke(area);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _currentColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            }
        }

        private void StrokeSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StrokeSizeCombo.SelectedIndex >= 0)
            {
                _currentStroke = StrokeSizeCombo.SelectedIndex switch
                {
                    0 => 2,
                    1 => 4,
                    2 => 8,
                    3 => 14,
                    _ => 4
                };
            }
        }

        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == ToolSelect) _currentTool = AnnotationType.Pen;
            else if (sender == ToolArrow) _currentTool = AnnotationType.Arrow;
            else if (sender == ToolRect) _currentTool = AnnotationType.Rectangle;
            else if (sender == ToolEllipse) _currentTool = AnnotationType.Ellipse;
            else if (sender == ToolPen) _currentTool = AnnotationType.Pen;
            else if (sender == ToolHighlighter) _currentTool = AnnotationType.Highlighter;
            else if (sender == ToolStepBadge) _currentTool = AnnotationType.StepBadge;
            else if (sender == ToolBlur) _currentTool = AnnotationType.Blur;
            else if (sender == ToolText) _currentTool = AnnotationType.Text;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                Copy_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Copy_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Save_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Undo_Click(this, new RoutedEventArgs());
            }
        }
    }
}
