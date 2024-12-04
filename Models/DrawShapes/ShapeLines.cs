using wpf522.Models.Enums;
using System.Windows;
using System.Windows.Media;

namespace wpf522.Models.DrawShapes
{
    public class ShapeLines : ShapeArea
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public string TypeName { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }

        public ShapeLines()
        {
            ShapeType = ShapeType.Lines;
        }
    }
}
