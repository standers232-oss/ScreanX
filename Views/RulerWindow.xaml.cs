using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using ScreanX.Services;
using Point = System.Windows.Point;

namespace ScreanX.Views
{
    public partial class RulerWindow : Window
    {
        private bool _isMeasuring = false;
        private Point _startPoint;
        private string _lastMeasurement = "";

        public RulerWindow()
        {
            InitializeComponent();
            var bounds = CaptureService.GetVirtualScreenBounds();
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isMeasuring = true;
                _startPoint = e.GetPosition(this);
                MeasureBorder.Visibility = Visibility.Visible;
                InfoBadge.Visibility = Visibility.Visible;
                UpdateMeasure(e.GetPosition(this));
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                Close();
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMeasuring)
            {
                UpdateMeasure(e.GetPosition(this));
            }
        }

        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isMeasuring)
            {
                _isMeasuring = false;
                if (!string.IsNullOrEmpty(_lastMeasurement))
                {
                    Clipboard.SetText(_lastMeasurement);
                }
            }
        }

        private void UpdateMeasure(Point current)
        {
            double x = Math.Min(_startPoint.X, current.X);
            double y = Math.Min(_startPoint.Y, current.Y);
            double w = Math.Abs(current.X - _startPoint.X);
            double h = Math.Abs(current.Y - _startPoint.Y);

            System.Windows.Controls.Canvas.SetLeft(MeasureBorder, x);
            System.Windows.Controls.Canvas.SetTop(MeasureBorder, y);
            MeasureBorder.Width = Math.Max(1, w);
            MeasureBorder.Height = Math.Max(1, h);

            DiagonalLine.X1 = 0;
            DiagonalLine.Y1 = 0;
            DiagonalLine.X2 = w;
            DiagonalLine.Y2 = h;

            double diag = Math.Sqrt(w * w + h * h);
            _lastMeasurement = $"{(int)w} × {(int)h} px (diag: {(int)diag} px)";
            DimensionsText.Text = $"{(int)w} × {(int)h} px";
            DiagonalText.Text = $"Диагональ: {diag:F1} px";

            double badgeX = x + w + 12;
            double badgeY = y + h + 12;
            if (badgeX + 160 > Width) badgeX = x - 170;
            if (badgeY + 80 > Height) badgeY = y - 90;

            System.Windows.Controls.Canvas.SetLeft(InfoBadge, Math.Max(10, badgeX));
            System.Windows.Controls.Canvas.SetTop(InfoBadge, Math.Max(10, badgeY));
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
