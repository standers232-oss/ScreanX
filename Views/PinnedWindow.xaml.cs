using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ScreanX.Services;

namespace ScreanX.Views
{
    public partial class PinnedWindow : Window
    {
        private readonly Bitmap _bitmap;

        public PinnedWindow(Bitmap bitmap, double left, double top)
        {
            InitializeComponent();
            _bitmap = bitmap;

            PinnedImage.Source = CaptureService.ToBitmapSource(_bitmap);
            Width = Math.Max(120, _bitmap.Width + 8);
            Height = Math.Max(80, _bitmap.Height + 8);
            Left = left;
            Top = top;
            Opacity = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            var pop = new DoubleAnimation(0.88, 1.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
            };

            BeginAnimation(OpacityProperty, fadeIn);
            BorderScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pop);
            BorderScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pop);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                Clipboard.SetImage(CaptureService.ToBitmapSource(_bitmap));
                ToastNotification.ShowToast("Изображение скопировано в буфер! 📋");
                Close();
            }
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Opacity = Math.Min(1.0, Opacity + 0.1);
            }
            else
            {
                Opacity = Math.Max(0.2, Opacity - 0.1);
            }
        }

        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
