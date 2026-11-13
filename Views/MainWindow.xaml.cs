using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ScreanX.Models;
using ScreanX.Services;

namespace ScreanX.Views
{
    public partial class MainWindow : Window
    {
        private readonly HotkeyService _hotkeyService = new();
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private bool _isRealExit = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize global hotkeys
            _hotkeyService.Initialize(this);
            RegisterHotkeys();

            // Initialize System Tray
            SetupSystemTray();

            // Load settings into UI
            LoadSettingsToUI();

            // Bind History
            HistoryItemsList.ItemsSource = HistoryService.Items;
            HistoryService.Items.CollectionChanged += (s, args) => UpdateHistoryUI();
            UpdateHistoryUI();

            // Check if launched with --tray or configured to start in tray
            if (App.StartInTray)
            {
                Hide();
                WindowState = WindowState.Minimized;
            }
            else
            {
                // Initial tab entrance animation
                AnimateTabEntrance(TabCapture, TabCaptureTransform, TabCaptureScale);
            }
        }

        private void UpdateHistoryUI()
        {
            int count = HistoryService.Items.Count;
            HistoryCountText.Text = $"Всего снимков: {count}";
            if (count == 0)
            {
                HistoryEmptyState.Visibility = Visibility.Visible;
                HistoryScrollViewer.Visibility = Visibility.Collapsed;
            }
            else
            {
                HistoryEmptyState.Visibility = Visibility.Collapsed;
                HistoryScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void SetupSystemTray()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Text = "ScreanX - Material 3 Screen Capture",
                    Visible = true,
                    Icon = SystemIcons.Application
                };

                var contextMenu = new System.Windows.Forms.ContextMenuStrip();
                contextMenu.Items.Add("📸 Снимок области (PrtScn)", null, (s, e) => Dispatcher.Invoke(StartRegionCapture));
                contextMenu.Items.Add("🖥️ Весь экран", null, (s, e) => Dispatcher.Invoke(StartFullscreenCapture));
                contextMenu.Items.Add("🪟 Активное окно", null, (s, e) => Dispatcher.Invoke(StartActiveWindowCapture));
                contextMenu.Items.Add("🎬 Запись экрана (Ctrl+Shift+R)", null, (s, e) => Dispatcher.Invoke(() => StartVideoRecording()));
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add("🎨 Пипетка цвета", null, (s, e) => Dispatcher.Invoke(StartColorPicker));
                contextMenu.Items.Add("📏 Экранная линейка", null, (s, e) => Dispatcher.Invoke(StartScreenRuler));
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add("⚙️ Открыть ScreanX", null, (s, e) => Dispatcher.Invoke(ShowAndRestore));
                contextMenu.Items.Add("❌ Выход", null, (s, e) => Dispatcher.Invoke(ExitApp));

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => Dispatcher.Invoke(ShowAndRestore);
            }
            catch { }
        }

        private void RegisterHotkeys()
        {
            // PrtScn -> Region capture
            _hotkeyService.Register(HotkeyService.MOD_NONE, HotkeyService.VK_SNAPSHOT, () => Dispatcher.Invoke(StartRegionCapture));

            // Ctrl + Shift + S -> Region capture
            _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, HotkeyService.VK_S, () => Dispatcher.Invoke(StartRegionCapture));

            // Shift + PrtScn -> Fullscreen capture
            _hotkeyService.Register(HotkeyService.MOD_SHIFT, HotkeyService.VK_SNAPSHOT, () => Dispatcher.Invoke(StartFullscreenCapture));

            // Alt + PrtScn -> Active window capture
            _hotkeyService.Register(HotkeyService.MOD_ALT, HotkeyService.VK_SNAPSHOT, () => Dispatcher.Invoke(StartActiveWindowCapture));

            // Ctrl + Shift + R -> Video recording
            _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, HotkeyService.VK_R, () => Dispatcher.Invoke(() => StartVideoRecording()));

            // Ctrl + Shift + C -> Color picker
            _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, HotkeyService.VK_C, () => Dispatcher.Invoke(StartColorPicker));

            // Ctrl + Shift + M -> Ruler
            _hotkeyService.Register(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, HotkeyService.VK_M, () => Dispatcher.Invoke(StartScreenRuler));
        }

        private void LoadSettingsToUI()
        {
            var s = SettingsService.Current;
            ToggleAutoCopy.IsChecked = s.AutoCopyClipboard;
            ToggleAutoSave.IsChecked = s.AutoSaveFile;
            ToggleSound.IsChecked = s.PlaySound;
            ToggleMinimizeTray.IsChecked = s.MinimizeToTray;
            ToggleStartWithWindows.IsChecked = s.StartWithWindows;
            ToggleStartInTray.IsChecked = s.StartMinimizedInTray;
            ToggleAutoEnhance.IsChecked = s.AutoEnhanceSmallSnippets;
            ToggleRecordCursor.IsChecked = s.RecordMouseCursor;
            ToggleExcludeControls.IsChecked = s.ExcludeControlsFromRecording;
            SavePathBox.Text = s.SaveDirectory;

            bool isDark = s.ThemeMode.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            ThemeDarkRadio.IsChecked = isDark;
            ThemeLightRadio.IsChecked = !isDark;
            ThemeIconPath.Data = (Geometry)FindResource(isDark ? "IconSun" : "IconMoon");

            PaletteCombo.SelectedIndex = s.ColorPalette.ToLowerInvariant() switch
            {
                "teal" => 1,
                "blue" => 2,
                "orange" => 3,
                _ => 0
            };

            FormatCombo.SelectedIndex = s.ImageFormat.ToUpperInvariant() switch
            {
                "JPEG" or "JPG" => 1,
                "BMP" => 2,
                _ => 0
            };

            VideoFormatCombo.SelectedIndex = s.VideoFormat.ToUpperInvariant() switch
            {
                "AVI" => 1,
                "GIF" => 2,
                _ => 0
            };

            VideoFpsCombo.SelectedIndex = s.RecordFps switch
            {
                15 => 0,
                24 => 1,
                60 => 3,
                _ => 2
            };
        }

        public async void StartRegionCapture()
        {
            bool wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
                await Task.Delay(180);
            }

            var desktopBmp = CaptureService.CaptureAllScreens();
            var snipper = new SnipperWindow(desktopBmp);
            snipper.OnCaptureComplete = (bmp) =>
            {
                if (wasVisible)
                {
                    Show();
                    WindowState = WindowState.Normal;
                }
                ToastNotification.ShowToast("Снимок области скопирован в буфер! 📸");
            };
            snipper.OnStartRecord = (area) =>
            {
                StartVideoRecording(area);
            };
            snipper.Closed += (s, e) =>
            {
                if (wasVisible && !snipper.IsVisible && !ScreenRecordService.Current.IsRecording)
                {
                    Show();
                }
            };
            snipper.ShowDialog();
        }

        public async void StartFullscreenCapture()
        {
            bool wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
                await Task.Delay(180);
            }

            using var bmp = CaptureService.CaptureAllScreens();
            ProcessCapturedBitmap(bmp);
            ToastNotification.ShowToast("Снимок всего экрана сохранён! 🖥️");

            if (wasVisible)
            {
                Show();
            }
        }

        public async void StartActiveWindowCapture()
        {
            bool wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
                await Task.Delay(250);
            }

            using var bmp = CaptureService.CaptureActiveWindow();
            if (bmp != null)
            {
                ProcessCapturedBitmap(bmp);
                ToastNotification.ShowToast("Снимок окна сохранён! 🪟");
            }

            if (wasVisible)
            {
                Show();
            }
        }

        private async void CaptureTimer_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            ToastNotification.ShowToast("Таймер 3 секунды... Приготовьтесь! ⏱️");
            await Task.Delay(3000);
            var desktopBmp = CaptureService.CaptureAllScreens();
            var snipper = new SnipperWindow(desktopBmp);
            snipper.OnCaptureComplete = (bmp) =>
            {
                ToastNotification.ShowToast("Снимок готов! 📸");
            };
            snipper.ShowDialog();
            Show();
        }

        private void ProcessCapturedBitmap(Bitmap bmp)
        {
            var settings = SettingsService.Current;

            if (settings.AutoCopyClipboard)
            {
                try
                {
                    var source = CaptureService.ToBitmapSource(bmp);
                    Clipboard.SetImage(source);
                }
                catch { }
            }

            if (settings.AutoSaveFile)
            {
                try
                {
                    string dir = settings.SaveDirectory;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string fileName = $"ScreanX_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{settings.ImageFormat.ToLowerInvariant()}";
                    string filePath = Path.Combine(dir, fileName);

                    ImageFormat format = settings.ImageFormat.ToUpperInvariant() switch
                    {
                        "JPEG" or "JPG" => ImageFormat.Jpeg,
                        "BMP" => ImageFormat.Bmp,
                        _ => ImageFormat.Png
                    };

                    bmp.Save(filePath, format);
                    HistoryService.AddItem(filePath, bmp.Width, bmp.Height);
                }
                catch { }
            }

            SoundService.PlayCaptureSound();
        }

        private void StartColorPicker()
        {
            bool wasVisible = IsVisible;
            Hide();
            var bmp = CaptureService.CaptureAllScreens();
            var picker = new ColorPickerWindow(bmp);
            picker.Closed += (s, e) =>
            {
                if (wasVisible) Show();
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (text.StartsWith("#"))
                    {
                        ToastNotification.ShowToast($"Цвет {text} скопирован! 🎨");
                    }
                }
            };
            picker.Show();
        }

        private void StartScreenRuler()
        {
            var ruler = new RulerWindow();
            ruler.Show();
        }

        #region Navigation and Smooth Tab Transitions
        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (TabCapture == null) return;

            TabCapture.Visibility = Visibility.Collapsed;
            TabHistory.Visibility = Visibility.Collapsed;
            TabTools.Visibility = Visibility.Collapsed;
            TabSettings.Visibility = Visibility.Collapsed;
            TabAbout.Visibility = Visibility.Collapsed;

            if (sender == NavCapture)
            {
                AnimateTabEntrance(TabCapture, TabCaptureTransform, TabCaptureScale);
            }
            else if (sender == NavHistory)
            {
                UpdateHistoryUI();
                AnimateTabEntrance(TabHistory, TabHistoryTransform, TabHistoryScale);
            }
            else if (sender == NavTools)
            {
                AnimateTabEntrance(TabTools, TabToolsTransform, TabToolsScale);
            }
            else if (sender == NavSettings)
            {
                AnimateTabEntrance(TabSettings, TabSettingsTransform, TabSettingsScale);
            }
            else if (sender == NavAbout)
            {
                AnimateTabEntrance(TabAbout, TabAboutTransform, TabAboutScale);
            }
        }

        private void AnimateTabEntrance(FrameworkElement tab, TranslateTransform transform, ScaleTransform? scale = null)
        {
            tab.Visibility = Visibility.Visible;
            tab.Opacity = 0;
            transform.Y = 18;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slideUp = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            tab.BeginAnimation(OpacityProperty, fadeIn);
            transform.BeginAnimation(TranslateTransform.YProperty, slideUp);

            if (scale != null)
            {
                scale.ScaleX = 0.97;
                scale.ScaleY = 0.97;
                var scaleAnim = new DoubleAnimation(0.97, 1.0, TimeSpan.FromMilliseconds(320))
                {
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
                };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }
        #endregion

        #region Video Recording
        public async void StartVideoRecording(Rectangle? area = null)
        {
            if (ScreenRecordService.Current.IsRecording)
            {
                ToastNotification.ShowToast("Запись экрана уже идет! ⏺️");
                return;
            }

            bool wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
                await Task.Delay(200);
            }

            Rectangle recordRect = area ?? System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? CaptureService.GetVirtualScreenBounds();

            // Ensure even width & height (standard for video encoders)
            int w = recordRect.Width;
            int h = recordRect.Height;
            if (w % 2 != 0) w--;
            if (h % 2 != 0) h--;
            if (w < 100 || h < 100)
            {
                w = Math.Max(w, 320);
                h = Math.Max(h, 240);
            }
            recordRect = new Rectangle(recordRect.Left, recordRect.Top, w, h);

            string dir = SettingsService.Current.SaveDirectory;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string format = SettingsService.Current.VideoFormat.ToLowerInvariant();
            string ext = format switch
            {
                "avi" => "avi",
                "gif" => "gif",
                _ => "mp4"
            };

            string fileName = $"ScreanX_Record_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{ext}";
            string filePath = Path.Combine(dir, fileName);

            Action<string>? onCompleted = null;
            onCompleted = (savedPath) =>
            {
                ScreenRecordService.Current.Completed -= onCompleted;
                Dispatcher.Invoke(() =>
                {
                    if (File.Exists(savedPath))
                    {
                        var fi = new FileInfo(savedPath);
                        double mb = fi.Length / (1024.0 * 1024.0);
                        ToastNotification.ShowToast($"Видео сохранено! 🎬 ({mb:F1} МБ)");
                        SoundService.PlayCaptureSound();
                    }
                    if (wasVisible)
                    {
                        Show();
                        WindowState = WindowState.Normal;
                    }
                });
            };

            ScreenRecordService.Current.Completed += onCompleted;

            var overlay = new RecordingOverlayWindow(recordRect);
            overlay.OnRecordingFinished = () =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (wasVisible && !ScreenRecordService.Current.IsRecording)
                    {
                        Show();
                    }
                });
            };

            int fps = SettingsService.Current.RecordFps;
            ScreenRecordService.Current.Start(recordRect, filePath, fps);
            overlay.Show();
        }
        #endregion

        #region Buttons and Events
        private void CaptureRegion_Click(object sender, RoutedEventArgs e) => StartRegionCapture();
        private void CaptureFullscreen_Click(object sender, RoutedEventArgs e) => StartFullscreenCapture();
        private void CaptureActiveWindow_Click(object sender, RoutedEventArgs e) => StartActiveWindowCapture();
        private void CaptureVideo_Click(object sender, RoutedEventArgs e) => StartVideoRecording();
        private void ToolColorPicker_Click(object sender, RoutedEventArgs e) => StartColorPicker();
        private void ToolRuler_Click(object sender, RoutedEventArgs e) => StartScreenRuler();

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/standers232-oss") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть браузер: {ex.Message}", "ScreanX");
            }
        }

        private void OpenTelegram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://t.me/CreatingX") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть браузер: {ex.Message}", "ScreanX");
            }
        }

        private void ResetHotkeys_Click(object sender, RoutedEventArgs e)
        {
            _hotkeyService.UnregisterAll();
            RegisterHotkeys();
            ToastNotification.ShowToast("Горячие клавиши сброшены по умолчанию! ⌨️");
        }

        private void ToolPinClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    using var ms = new MemoryStream();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var bmp = new Bitmap(ms);
                    var pinned = new PinnedWindow(bmp, 200, 200);
                    pinned.Show();
                    ToastNotification.ShowToast("Картинка закреплена поверх окон! 📌");
                }
            }
            else
            {
                MessageBox.Show("В буфере обмена нет изображения. Сделайте сначала снимок или скопируйте картинку.", "ScreanX", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenSaveFolder_Click(object sender, RoutedEventArgs e)
        {
            string dir = SettingsService.Current.SaveDirectory;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить список истории снимков?", "ScreanX", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                HistoryService.ClearAll();
                UpdateHistoryUI();
                ToastNotification.ShowToast("История снимков очищена");
            }
        }

        private void HistoryItemCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HistoryItem item && File.Exists(item.FilePath))
            {
                try
                {
                    using var bmp = new Bitmap(item.FilePath);
                    Clipboard.SetImage(CaptureService.ToBitmapSource(bmp));
                    SoundService.PlayCaptureSound();
                    ToastNotification.ShowToast("Снимок скопирован в буфер! 📋");
                }
                catch { }
            }
        }

        private void HistoryItemPin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HistoryItem item && File.Exists(item.FilePath))
            {
                var bmp = new Bitmap(item.FilePath);
                var pinned = new PinnedWindow(bmp, 150, 150);
                pinned.Show();
                ToastNotification.ShowToast("Снимок закреплен на экране! 📌");
            }
        }

        private void HistoryItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HistoryItem item && File.Exists(item.FilePath))
            {
                Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
        }

        private void HistoryItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HistoryItem item)
            {
                HistoryService.RemoveItem(item);
                try { File.Delete(item.FilePath); } catch { }
                UpdateHistoryUI();
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = SettingsService.Current.SaveDirectory;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsService.Current.SaveDirectory = dialog.SelectedPath;
                SavePathBox.Text = dialog.SelectedPath;
                SettingsService.Save();
                ToastNotification.ShowToast("Папка сохранения обновлена");
            }
        }

        private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormatCombo.SelectedIndex >= 0)
            {
                SettingsService.Current.ImageFormat = FormatCombo.SelectedIndex switch
                {
                    1 => "JPEG",
                    2 => "BMP",
                    _ => "PNG"
                };
                SettingsService.Save();
            }
        }

        private void SettingToggle_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.AutoCopyClipboard = ToggleAutoCopy.IsChecked == true;
            s.AutoSaveFile = ToggleAutoSave.IsChecked == true;
            s.PlaySound = ToggleSound.IsChecked == true;
            s.MinimizeToTray = ToggleMinimizeTray.IsChecked == true;
            s.AutoEnhanceSmallSnippets = ToggleAutoEnhance.IsChecked == true;
            s.RecordMouseCursor = ToggleRecordCursor.IsChecked == true;
            s.ExcludeControlsFromRecording = ToggleExcludeControls.IsChecked == true;
            s.StartWithWindows = ToggleStartWithWindows.IsChecked == true;
            s.StartMinimizedInTray = ToggleStartInTray.IsChecked == true;
            StartupService.SetStartup(s.StartWithWindows, s.StartMinimizedInTray);
            SettingsService.Save();
        }

        private void VideoFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoFormatCombo.SelectedIndex >= 0)
            {
                SettingsService.Current.VideoFormat = VideoFormatCombo.SelectedIndex switch
                {
                    1 => "AVI",
                    2 => "GIF",
                    _ => "MP4"
                };
                SettingsService.Save();
            }
        }

        private void VideoFpsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoFpsCombo.SelectedIndex >= 0)
            {
                SettingsService.Current.RecordFps = VideoFpsCombo.SelectedIndex switch
                {
                    0 => 15,
                    1 => 24,
                    3 => 60,
                    _ => 30
                };
                SettingsService.Save();
            }
        }

        private void PaletteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaletteCombo.SelectedIndex >= 0)
            {
                string pal = PaletteCombo.SelectedIndex switch
                {
                    1 => "Teal",
                    2 => "Blue",
                    3 => "Orange",
                    _ => "Purple"
                };
                ChangePalette(pal);
            }
        }

        private void Palette_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string pal)
            {
                ChangePalette(pal);
                PaletteCombo.SelectedIndex = pal.ToLowerInvariant() switch
                {
                    "teal" => 1,
                    "blue" => 2,
                    "orange" => 3,
                    _ => 0
                };
            }
        }

        private void ChangePalette(string palette)
        {
            SettingsService.Current.ColorPalette = palette;
            ThemeService.ApplyTheme(SettingsService.Current.ThemeMode, palette);
            SettingsService.Save();
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            string mode = ThemeDarkRadio.IsChecked == true ? "Dark" : "Light";
            ApplyThemeMode(mode);
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = SettingsService.Current.ThemeMode.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            string newMode = isDark ? "Light" : "Dark";
            ApplyThemeMode(newMode);
            ThemeDarkRadio.IsChecked = newMode == "Dark";
            ThemeLightRadio.IsChecked = newMode == "Light";
        }

        private void ApplyThemeMode(string mode)
        {
            SettingsService.Current.ThemeMode = mode;
            ThemeService.ApplyTheme(mode, SettingsService.Current.ColorPalette);
            ThemeIconPath.Data = (Geometry)FindResource(mode == "Dark" ? "IconSun" : "IconMoon");
            SettingsService.Save();
        }
        #endregion

        #region Title Bar & Window Chrome
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsService.Current.MinimizeToTray)
            {
                Hide();
                ToastNotification.ShowToast("ScreanX свернут в системный трей");
            }
            else
            {
                ExitApp();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isRealExit && SettingsService.Current.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
            }
        }

        public void ShowAndRestore()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void ExitApp()
        {
            _isRealExit = true;
            _notifyIcon?.Dispose();
            _hotkeyService.Dispose();
            Application.Current.Shutdown();
        }
        #endregion
    }
}
