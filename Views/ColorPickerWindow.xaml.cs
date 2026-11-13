using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScreanX.Services;
using Color = System.Windows.Media.Color;
using Rectangle = System.Drawing.Rectangle;

namespace ScreanX.Views
{
    public partial class ColorPickerWindow : Window
    {
        private readonly Bitmap _desktopBmp;
        private readonly Rectangle _screenBounds;
        private string _currentHex = "#FFFFFF";

        public ColorPickerWindow(Bitmap desktopBmp)
        {
            InitializeComponent();
            _desktopBmp = desktopBmp;
            _screenBounds = CaptureService.GetVirtualScreenBounds();

            Left = _screenBounds.Left;
            Top = _screenBounds.Top;
            Width = _screenBounds.Width;
            Height = _screenBounds.Height;

            BgImage.Source = CaptureService.ToBitmapSource(_desktopBmp);
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point pt = e.GetPosition(this);
            double cardX = pt.X + 20;
            double cardY = pt.Y + 20;

            if (cardX + 200 > Width) cardX = pt.X - 210;
            if (cardY + 240 > Height) cardY = pt.Y - 240;

            System.Windows.Controls.Canvas.SetLeft(LoupeCard, cardX);
            System.Windows.Controls.Canvas.SetTop(LoupeCard, cardY);

            int px = (int)Math.Clamp(pt.X, 0, _desktopBmp.Width - 1);
            int py = (int)Math.Clamp(pt.Y, 0, _desktopBmp.Height - 1);

            System.Drawing.Color c = _desktopBmp.GetPixel(px, py);
            _currentHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            HexText.Text = _currentHex;
            RgbText.Text = $"RGB({c.R}, {c.G}, {c.B})";
            ColorBox.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));

            // Extract 15x15 pixel grid
            int cropSize = 15;
            int cx = Math.Clamp(px - cropSize / 2, 0, _desktopBmp.Width - cropSize);
            int cy = Math.Clamp(py - cropSize / 2, 0, _desktopBmp.Height - cropSize);
            using var crop = CaptureService.CropBitmap(_desktopBmp, new Rectangle(cx, cy, cropSize, cropSize));
            ZoomImage.Source = CaptureService.ToBitmapSource(crop);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Clipboard.SetText(_currentHex);
                SoundService.PlayCaptureSound();
                Close();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                Close();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter || e.Key == Key.C)
            {
                Clipboard.SetText(_currentHex);
                SoundService.PlayCaptureSound();
                Close();
            }
        }
    }
}
