using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace wpf522.Expends
{
    public static class WindowStructExpend
    {






        public static bool IsContains(this Rect rect, Rect other)
        {
            return Math.Min(rect.X, other.X) == rect.X &&
                Math.Min(rect.Y, other.Y) == rect.Y &&
                Math.Max(rect.X + rect.Width, other.X + other.Width) == rect.X + rect.Width &&
                Math.Max(rect.Y + rect.Height, other.Y + other.Height) == rect.Y + rect.Height;
        }






        public static bool IsContains(this Rect rect, Polygon polygon)
        {
            return rect.IsContains(polygon.GetPolygonWithRect());
        }






        public static bool IsContains(this Rect rect, Rectangle rectangle)
        {
            return rect.IsContains(new Rect(Canvas.GetLeft(rectangle), Canvas.GetTop(rectangle), rectangle.Width, rectangle.Height));
        }





        public static Rect GetPolygonWithRect(this Polygon polygon)
        {
            var point = new Point(Canvas.GetLeft(polygon), Canvas.GetTop(polygon));
            double maxx = 0;
            double maxy = 0;
            foreach (var item in polygon.Points)
            {
                var x = item.X + point.X;
                var y = item.Y + point.Y;

                if (x > maxx)
                {
                    maxx = x;
                }
                if (y > maxy)
                {
                    maxy = y;
                }
            }
             
            return new Rect(point.X, point.Y, maxx - point.X, maxy - point.Y);
        }




        public static bool IsContains(this Rect rect, Line line)
        {
            var p1 = new Point(line.X1, line.Y1);
            var p2 = new Point(line.X2, line.Y2);
            return rect.Contains(p1) || rect.Contains(p2) || LineIntersectsRect(p1, p2, rect);
        }
        private static bool LineIntersectsRect(Point p1, Point p2, Rect rect)
        {

            var rectPoints = new[]
            {
        new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top),
        new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom)
        };

            for (int i = 0; i < 4; i++)
            {
                if (LineIntersectsLine(p1, p2, rectPoints[i], rectPoints[(i + 1) % 4]))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool LineIntersectsLine(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
        {
            double detL1 = (l1p1.X - l1p2.X) * (l2p1.Y - l2p2.Y) - (l1p1.Y - l1p2.Y) * (l2p1.X - l2p2.X);
            if (detL1 == 0) return false;
            double detL2 = (l2p1.X - l2p2.X) * (l1p1.Y - l1p2.Y) - (l2p1.Y - l2p2.Y) * (l1p1.X - l1p2.X);
            if (detL2 == 0) return false;
            double x = ((l2p1.X - l2p2.X) * (l1p1.X * l1p2.Y - l1p1.Y * l1p2.X) - (l1p1.X - l1p2.X) * (l2p1.X * l2p2.Y - l2p1.Y * l2p2.X)) / detL1;
            double y = ((l2p1.Y - l2p2.Y) * (l1p1.X * l1p2.Y - l1p1.Y * l1p2.X) - (l1p1.Y - l1p2.Y) * (l2p1.X * l2p2.Y - l2p1.Y * l2p2.X)) / detL2;
            return x >= Math.Min(l1p1.X, l1p2.X) && x <= Math.Max(l1p1.X, l1p2.X) && x >= Math.Min(l2p1.X, l2p2.X) && x <= Math.Max(l2p1.X, l2p2.X)
                && y >= Math.Min(l1p1.Y, l1p2.Y) && y <= Math.Max(l1p1.Y, l1p2.Y) && y >= Math.Min(l2p1.Y, l2p2.Y) && y <= Math.Max(l2p1.Y, l2p2.Y);
        }





        public static string ColorToHexARGB(this Color color)
        {
            return color.ToString();
        }
    }
}

