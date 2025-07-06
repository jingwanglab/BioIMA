using wpf522.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using wpf522.Models.Enums;


namespace wpf522.Method
{
    public static class InkCanvasMethod
    {
        public static Color ColorDefault { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xA5, 0x00);
        public static Color ColorMask { get; set; } = Color.FromArgb(0x99, 0x07, 0xAA, 0xE5);
        public static Brush StrokeBrushDefault { get; set; } = new SolidColorBrush(ColorDefault);
        public static Brush StrokeBrushMask { get; set; } = new SolidColorBrush(ColorMask);
        public static double InkStrokeThickness { get; set; } = 10;



        public static DrawingAttributes SetInkAttributesDefault(double thickness = 2)
        {
            DrawingAttributes attributes = new DrawingAttributes
            {
                FitToCurve = false,
                Width = thickness,
                Height = thickness,
                Color = ColorDefault,
                StylusTip = StylusTip.Ellipse,
                IsHighlighter = true,
                IgnorePressure = true,
            };
            return attributes;
        }





        public static DrawingAttributes SetInkAttributesMask()
        {
            DrawingAttributes attributes = new DrawingAttributes
            {
                FitToCurve = false,
                Width = InkStrokeThickness,
                Height = InkStrokeThickness,
                Color = ColorMask,
                StylusTip = StylusTip.Ellipse,
                IsHighlighter = true,
                IgnorePressure = true,
            };
            return attributes;
        }




        public static Pen SetPenSolid(double thickness = 2)
        {
            Pen pen = new Pen
            {
                Brush = StrokeBrushDefault,
                Thickness = thickness,
                DashCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 0.0
            };
            return pen;
        }




        public static Pen SetPenMask(double thickness = 10)
        {
            Pen pen = new Pen
            {
                Brush = StrokeBrushMask,
                Thickness = thickness,
                DashCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 0.0
            };
            return pen;
        }




        public static Pen SetPenDotted(double thickness = 1)
        {
            Pen pen = new Pen
            {
                Brush = StrokeBrushDefault,
                Thickness = thickness,
                DashStyle = new DashStyle(new double[] { 2.0, 2.0 }, 0.0),
                DashCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 0.0
            };
            return pen;
        }




        public static Pen SetPenPoint(double thickness = 6)
        {
            Pen pen = new Pen
            {
                Brush = StrokeBrushDefault,
                Thickness = thickness,
                DashCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 0.0
            };
            return pen;
        }





        public static Pen SetPenPointMask()
        {
            Pen pen = new Pen
            {
                Brush = StrokeBrushMask,
                Thickness = InkStrokeThickness,
                DashCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 0.0
            };
            return pen;
        }






        public static double GetDistancePP(Point p1, Point p2)
        {
            return Math.Sqrt(((p1.X - p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }
        public static List<double> GetDistancePP(StylusPointCollection sps, Point p)
        {
            List<double> dists = new List<double>();
            for (int i = 0; i < sps.Count; i++)
            {
                dists.Add(GetDistancePP((Point)sps[i], p));
            }
            return dists;
        }







        public static bool GetPointInPolygon(this StylusPointCollection points, Point pt, bool noneZeroMode = false)
        {
            int ptNum = points.Count;
            if (ptNum < 3)
            {
                return false;
            }
            int j = ptNum - 1;
            bool oddNodes = false;
            int zeroState = 0;
            for (int k = 0; k < ptNum; k++)
            {
                Point ptK = (Point)points[k];
                Point ptJ = (Point)points[j];
                if (((ptK.Y > pt.Y) != (ptJ.Y > pt.Y)) && (pt.X < (ptJ.X - ptK.X) * (pt.Y - ptK.Y) / (ptJ.Y - ptK.Y) + ptK.X))
                {
                    oddNodes = !oddNodes;
                    if (ptK.Y > ptJ.Y)
                    {
                        zeroState++;
                    }
                    else
                    {
                        zeroState--;
                    }
                }
                j = k;
            }
            return noneZeroMode ? zeroState != 0 : oddNodes;
        }





        public static Point GetPolygonCenter(this StylusPointCollection points)
        {
            Point point = new Point(0, 0);
            double area = 0;
            double x0;
            double y0;
            double x1;
            double y1;
            double temp;
            for (int i = 0; i < points.Count - 1; i++)
            {
                x0 = points[i].X;
                y0 = points[i].Y;
                x1 = points[i + 1].X;
                y1 = points[i + 1].Y;
                temp = (x0 * y1) - (x1 * y0);
                area += temp;
                point.X += (x0 + x1) * temp;
                point.Y += (y0 + y1) * temp;
            }
            x0 = points[points.Count - 1].X;
            y0 = points[points.Count - 1].Y;
            x1 = points[0].X;
            y1 = points[0].Y;
            temp = (x0 * y1) - (x1 * y0);
            area += temp;
            point.X += (x0 + x1) * temp;
            point.Y += (y0 + y1) * temp;
            point.X /= 3 * area;
            point.Y /= 3 * area;
            return point;
        }





        public static PointCollection StylusPointsConverter(this StylusPointCollection points)
        {
            PointCollection pts = new PointCollection();
            for (int i = 0; i < points.Count; i++)
            {
                pts.Add(new Point(points[i].X, points[i].Y));
            }
            return pts;
        }







        public static double GetPointAngle(Point C, Point A, Point B)
        {
            double a = GetDistancePP(B, C);
            double b = GetDistancePP(A, C);
            double c = GetDistancePP(A, B);
            double cTheta = ((a * a) + (b * b) - (c * c)) / (2 * a * b);
            cTheta = Math.Min(1, cTheta);
            return Math.Acos(cTheta) * 180 / Math.PI;
        }







        public static bool GetRotateDirection(Point p0, Point p1, Point p2)
        {
            Vector vector1 = p1 - p0;
            Vector vector2 = p2 - p0;

            bool direction = (vector1.X * vector2.Y) - (vector1.Y * vector2.X) > 0;
            return direction;
        }







        public static double GetDistancePL(Point p0, Point p1, Point p2)
        {
            double dist = 0;
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;

            if (Math.Abs(x1 - x2) < 1e-6)
            {
                dist = Math.Abs(p0.X - x1);
            }
            else
            {


                double k = (y2 - y1) / (x2 - x1);
                double b = (x2 * y1 - x1 * y2) / (x2 - x1);
                dist = Math.Abs(k * p0.X - p0.Y + b) / Math.Sqrt(k * k + 1);







            }
            return dist;
        }






        public static ModuleEditType GetCaliperEditType(this Stroke stroke, Point p)
        {
            ModuleEditType mode = ModuleEditType.None;

            StylusPointCollection sps = stroke.StylusPoints.Clone();
            double threshold = 5;

            if (GetPointInPolygon(sps, p))
            {
                mode = ModuleEditType.Move;
            }

            for (int i = 0; i < sps.Count; i++)
            {
                if (GetDistancePP(p, (Point)sps[i]) <= 2 * threshold)
                {
                    mode = ModuleEditType.Rotate;
                }
            }

            if (GetDistancePP(p, (Point)sps[0]) <= threshold || GetDistancePP(p, (Point)sps[1]) <= threshold || GetDistancePP(p, (Point)sps[2]) <= threshold || GetDistancePP(p, (Point)sps[3]) <= threshold)
            {
                mode = ModuleEditType.Resize;
            }

            return mode;
        }







        public static ModuleEditType GetMeasureTools(this Stroke stroke, Point p, EnumMeasureTools tool = EnumMeasureTools.distance)
        {
            ModuleEditType mode = ModuleEditType.None;

            StylusPointCollection sps = stroke.StylusPoints.Clone();
            double threshold = 5;

            if (tool == EnumMeasureTools.distance)
            {

                if (GetDistancePL(p, (Point)sps[0], (Point)sps[1]) < threshold)
                {
                    mode = ModuleEditType.Move;
                }

                if (GetDistancePP(p, (Point)sps[0]) <= threshold || GetDistancePP(p, (Point)sps[1]) <= threshold)
                {
                    mode = ModuleEditType.Resize;
                }
            }
            else if (tool == EnumMeasureTools.angle)
            {

                if (GetPointInPolygon(sps, p))
                {
                    mode = ModuleEditType.Move;
                }


                if (GetDistancePP(p, (Point)sps[0]) <= threshold || GetDistancePP(p, (Point)sps[1]) <= threshold || GetDistancePP(p, (Point)sps[2]) <= threshold)
                {
                    mode = ModuleEditType.Resize;
                }
            }

            return mode;
        }






        public static CustomArrowDistance CreateArrowDistance(Point point1, Point point2)
        {
            StylusPointCollection points = new StylusPointCollection()
            {
                new StylusPoint(point1.X, point1.Y),
                new StylusPoint(point2.X, point2.Y),
            };
            CustomArrowDistance stroke = new CustomArrowDistance(new StylusPointCollection(points))
            {
                DrawingAttributes = SetInkAttributesDefault(),
            };
            return stroke;
        }
    }
}

