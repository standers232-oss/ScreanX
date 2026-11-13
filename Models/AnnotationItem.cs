using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ScreanX.Models
{
    public enum AnnotationType
    {
        Pen,
        Highlighter,
        Arrow,
        Rectangle,
        FilledRectangle,
        Ellipse,
        FilledEllipse,
        StepBadge,
        Blur,
        Text
    }

    public class AnnotationItem
    {
        public AnnotationType Type { get; set; }
        public System.Windows.Media.Color StrokeColor { get; set; } = Colors.Red;
        public System.Windows.Media.Color FillColor { get; set; } = Colors.Transparent;
        public double StrokeThickness { get; set; } = 4;
        
        // For lines/arrows/shapes:
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }

        // For freehand pen / highlighter:
        public List<System.Windows.Point> Points { get; set; } = new();

        // For Text and StepBadge:
        public string Text { get; set; } = string.Empty;
        public double FontSize { get; set; } = 16;
        public int StepNumber { get; set; } = 1;

        // Rectangle bounds helper
        public Rect Bounds => new Rect(
            System.Math.Min(StartPoint.X, EndPoint.X),
            System.Math.Min(StartPoint.Y, EndPoint.Y),
            System.Math.Max(1, System.Math.Abs(EndPoint.X - StartPoint.X)),
            System.Math.Max(1, System.Math.Abs(EndPoint.Y - StartPoint.Y))
        );
    }
}
