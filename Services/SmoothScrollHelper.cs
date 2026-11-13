using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ScreanX.Services
{
    public static class SmoothScrollHelper
    {
        public static readonly DependencyProperty SmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "SmoothScroll",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnSmoothScrollChanged));

        public static bool GetSmoothScroll(DependencyObject obj) => (bool)obj.GetValue(SmoothScrollProperty);
        public static void SetSmoothScroll(DependencyObject obj, bool value) => obj.SetValue(SmoothScrollProperty, value);

        public static readonly DependencyProperty TargetOffsetProperty =
            DependencyProperty.RegisterAttached(
                "TargetOffset",
                typeof(double),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(0.0));

        public static double GetTargetOffset(DependencyObject obj) => (double)obj.GetValue(TargetOffsetProperty);
        public static void SetTargetOffset(DependencyObject obj, double value) => obj.SetValue(TargetOffsetProperty, value);

        public static readonly DependencyProperty CurrentOffsetProperty =
            DependencyProperty.RegisterAttached(
                "CurrentOffset",
                typeof(double),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(0.0, OnCurrentOffsetChanged));

        public static double GetCurrentOffset(DependencyObject obj) => (double)obj.GetValue(CurrentOffsetProperty);
        public static void SetCurrentOffset(DependencyObject obj, double value) => obj.SetValue(CurrentOffsetProperty, value);

        private static void OnCurrentOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                sv.ScrollToVerticalOffset((double)e.NewValue);
            }
        }

        private static void OnSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                if ((bool)e.NewValue)
                {
                    sv.PreviewMouseWheel += Sv_PreviewMouseWheel;
                }
                else
                {
                    sv.PreviewMouseWheel -= Sv_PreviewMouseWheel;
                }
            }
        }

        private static void Sv_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv || sv.ScrollableHeight <= 0) return;

            e.Handled = true;

            double currentTarget = GetTargetOffset(sv);
            if (double.IsNaN(currentTarget) || Math.Abs(currentTarget - sv.VerticalOffset) > 300 || currentTarget < 0 || currentTarget > sv.ScrollableHeight)
            {
                currentTarget = sv.VerticalOffset;
            }

            // Smooth scroll delta with fluid dampening
            double delta = e.Delta * 0.95;
            double newTarget = Math.Max(0, Math.Min(sv.ScrollableHeight, currentTarget - delta));
            SetTargetOffset(sv, newTarget);

            // Animate smoothly using CubicEase for buttery-smooth deceleration
            var anim = new DoubleAnimation
            {
                From = sv.VerticalOffset,
                To = newTarget,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            sv.BeginAnimation(CurrentOffsetProperty, anim);
        }

        public static void EnableFor(ScrollViewer sv)
        {
            SetSmoothScroll(sv, true);
        }
    }
}
