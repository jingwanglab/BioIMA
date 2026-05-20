using System.Collections.Generic;
using System.Linq;
using global::Avalonia;
using global::Avalonia.Collections;

namespace BioIMA.Avalonia.Models;

public class OverlayShapeViewModel
{
    public string ShapeType { get; set; } = string.Empty;
    public string Stroke { get; set; } = "#FFFFFF";
    public string Fill { get; set; } = "#00000000";

    public List<OverlayPointViewModel> Points { get; set; } = new();

    public AvaloniaList<Point> PolygonPoints =>
        new(Points.Select(p => new Point(p.X, p.Y)));

    public AvaloniaList<Point> PolylinePoints =>
        new(Points.Select(p => new Point(p.X, p.Y)));

    // angle: p1 -> p2 -> p3，两条边
    public AvaloniaList<Point> AnglePolylinePoints
    {
        get
        {
            if (Points.Count < 3)
                return new AvaloniaList<Point>();

            return new AvaloniaList<Point>
            {
                new Point(Points[0].X, Points[0].Y),
                new Point(Points[1].X, Points[1].Y),
                new Point(Points[2].X, Points[2].Y)
            };
        }
    }

    // 第一条边的延长线：从 p2 朝 p1 反方向延伸
    public AvaloniaList<Point> AngleGuideLinePoints
    {
        get
        {
            if (Points.Count < 3)
                return new AvaloniaList<Point>();

            var p1 = Points[0];
            var p2 = Points[1];

            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            var len = System.Math.Sqrt(dx * dx + dy * dy);

            if (len <= 0.0001)
                return new AvaloniaList<Point>();

            var ux = dx / len;
            var uy = dy / len;

            // 延长长度可调
            var ext = 40.0;

            var ex = p2.X - ux * ext;
            var ey = p2.Y - uy * ext;

            return new AvaloniaList<Point>
            {
                new Point(p2.X, p2.Y),
                new Point(ex, ey)
            };
        }
    }

    public string AngleText { get; set; } = string.Empty;

    public double AngleTextLeft
    {
        get
        {
            if (Points.Count < 3) return 0;
            return Points[1].X + 8;
        }
    }

    public double AngleTextTop
    {
        get
        {
            if (Points.Count < 3) return 0;
            return Points[1].Y + 8;
        }
    }

    public string MeasureText { get; set; } = string.Empty;

    public double MeasureTextLeft
    {
        get
        {
            if (Points.Count < 2) return 0;
            return (Points[0].X + Points[1].X) / 2.0 + 8;
        }
    }

    public double MeasureTextTop
    {
        get
        {
            if (Points.Count < 2) return 0;
            return (Points[0].Y + Points[1].Y) / 2.0 - 22;
        }
    }
}