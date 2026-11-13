using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScreanX.Services;

namespace ScreanX.Views
{
    public partial class RecordingOverlayWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        private readonly System.Drawing.Rectangle _recordArea;
        private readonly System.Drawing.Rectangle _virtualBounds;
        private bool _isPaused = false;
        private bool _isDragging = false;
        private System.Windows.Point _dragStart;
        private double _barStartX;
        private double _barStartY;

        public Action? OnRecordingFinished { get; set; }

        public RecordingOverlayWindow(System.Drawing.Rectangle recordArea)
        {
            InitializeComponent();
            _recordArea = recordArea;
            _virtualBounds = CaptureService.GetVirtualScreenBounds();

            Left = _virtualBounds.Left;
            Top = _virtualBounds.Top;
            Width = _virtualBounds.Width;
            Height = _virtualBounds.Height;

            // Offset to window coordinate system
            double rx = _recordArea.Left - _virtualBounds.Left;
            double ry = _recordArea.Top - _virtualBounds.Top;

            Canvas.SetLeft(RecordingBorder, rx);
            Canvas.SetTop(RecordingBorder, ry);
            RecordingBorder.Width = _recordArea.Width;
            RecordingBorder.Height = _recordArea.Height;

            InfoText.Text = $"{_recordArea.Width} × {_recordArea.Height} • REC";

            // Position control bar
            double barWidth = 340;
            double barHeight = 44;
            double barX = rx + (_recordArea.Width - barWidth) / 2.0;
            barX = Math.Max(12, Math.Min(Width - barWidth - 12, barX));

            double barY = ry + _recordArea.Height + 14;
            if (barY + barHeight > Height - 12)
            {
                barY = ry - barHeight - 14;
            }
            if (barY < 12)
            {
                barY = ry + 14;
            }

            Canvas.SetLeft(ControlBar, barX);
            Canvas.SetTop(ControlBar, barY);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero && SettingsService.Current.ExcludeControlsFromRecording)
                {
                    // WDA_EXCLUDEFROMCAPTURE makes the window completely invisible to screen capture & video recording!
                    SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                }
            }
            catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Pulsing Red Dot animation
            var pulse = new DoubleAnimation(1.0, 0.25, TimeSpan.FromMilliseconds(600))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            RecDot.BeginAnimation(OpacityProperty, pulse);

            // Subscribe to ticker
            ScreenRecordService.Current.Tick += OnTick;
        }

        private void OnTick(TimeSpan elapsed)
        {
            Dispatcher.Invoke(() =>
            {
                TimerText.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            });
        }

        private void ControlBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.OriginalSource is DependencyObject dep)
                {
                    // Do not drag if clicking buttons
                    DependencyObject? current = dep;
                    while (current != null && current != ControlBar)
                    {
                        if (current is Button) return;
                        current = VisualTreeHelper.GetParent(current);
                    }
                }

                _isDragging = true;
                _dragStart = e.GetPosition(this);
                _barStartX = Canvas.GetLeft(ControlBar);
                _barStartY = Canvas.GetTop(ControlBar);
                ControlBar.CaptureMouse();
            }
        }

        private void ControlBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(this);
                double dx = current.X - _dragStart.X;
                double dy = current.Y - _dragStart.Y;

                double newX = Math.Max(8, Math.Min(Width - ControlBar.ActualWidth - 8, _barStartX + dx));
                double newY = Math.Max(8, Math.Min(Height - ControlBar.ActualHeight - 8, _barStartY + dy));

                Canvas.SetLeft(ControlBar, newX);
                Canvas.SetTop(ControlBar, newY);
            }
        }

        private void ControlBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ControlBar.ReleaseMouseCapture();
            }
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused;
            if (_isPaused)
            {
                ScreenRecordService.Current.Pause();
                PauseIcon.Data = (Geometry)FindResource("IconRecord");
                PauseBtn.ToolTip = "Возобновить запись";
                InfoText.Text = $"{_recordArea.Width} × {_recordArea.Height} • ПАУЗА";
            }
            else
            {
                ScreenRecordService.Current.Resume();
                PauseIcon.Data = (Geometry)FindResource("IconPause");
                PauseBtn.ToolTip = "Пауза";
                InfoText.Text = $"{_recordArea.Width} × {_recordArea.Height} • REC";
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            ScreenRecordService.Current.Tick -= OnTick;
            ScreenRecordService.Current.Stop();
            OnRecordingFinished?.Invoke();
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ScreenRecordService.Current.Tick -= OnTick;
            ScreenRecordService.Current.Cancel();
            OnRecordingFinished?.Invoke();
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelBtn_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Enter)
            {
                StopBtn_Click(this, new RoutedEventArgs());
            }
        }
    }
}
