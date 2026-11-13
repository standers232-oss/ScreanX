using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScreanX.Views
{
    public partial class ToastNotification : Window
    {
        public ToastNotification(string message, Geometry? icon = null)
        {
            InitializeComponent();
            ToastMessage.Text = message;
            if (icon != null)
            {
                ToastIcon.Data = icon;
            }

            // Position at bottom-right of primary screen
            var screen = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea 
                ?? System.Windows.Forms.SystemInformation.WorkingArea;

            Left = screen.Right - 360;
            Top = screen.Bottom - 110;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Slide in & fade in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            var slideUp = new DoubleAnimation(25, 0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            ToastCard.BeginAnimation(OpacityProperty, fadeIn);
            ToastTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);

            await Task.Delay(2400);

            // Fade out & slide down
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            var slideDown = new DoubleAnimation(0, 15, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (s, args) => Close();

            ToastCard.BeginAnimation(OpacityProperty, fadeOut);
            ToastTransform.BeginAnimation(TranslateTransform.YProperty, slideDown);
        }

        public static void ShowToast(string message, Geometry? icon = null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var toast = new ToastNotification(message, icon);
                    toast.Show();
                }
                catch { }
            });
        }
    }
}
