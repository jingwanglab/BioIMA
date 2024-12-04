//using agmeasure;
//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

//namespace wpf522.Models.DrawShapes
//{

//    public partial class AngleMeasure : Window
//    {// 在角度测量类中引入对 MainModel 的引用
//        private MainModel mainModel;
//        //在构造函数中为 mainModel 实例化
//        private InkCanvas inkCanvasMeasure;///在这里声明 inkCanvasMeasure 变量，以便在整个类中访问
//        private ViewModel viewModel;
//        System.Windows.Point iniP;
//        System.Windows.Point line1P;
//        System.Windows.Point line2P;
//        private MainModel viewModel3;
//        public AngleMeasure()
//        {
//            InitializeComponent();
//            viewModel3 = mainModel;
//            this.DataContext = viewModel3;
//            DrawingAttributes drawingAttributes = new DrawingAttributes
//            {
//                Color = Colors.Red,
//                Width = 2,
//                Height = 2,
//                StylusTip = StylusTip.Rectangle,
//                FitToCurve = true,
//                IsHighlighter = false,
//                IgnorePressure = true,

//            };
//            inkCanvasMeasure.DefaultDrawingAttributes = drawingAttributes;

//            viewModel = new ViewModel
//            {
//                MeaInfo = "角度测量模式······",
//                InkStrokes = new StrokeCollection(),
//            };

//            DataContext = viewModel;
//        }

//        private void InitializeComponent()
//        {
//            throw new NotImplementedException();
//        }

//        private void DrawAxiesCircle()
//        {
//            //Draw Line
//            List<System.Windows.Point> pointList = new List<System.Windows.Point>
//                        {
//                            new System.Windows.Point(iniP.X, iniP.Y),
//                            new System.Windows.Point(line2P.X, line2P.Y),
//                        };
//            StylusPointCollection point = new StylusPointCollection(pointList);
//            Stroke stroke = new Stroke(point)
//            {
//                DrawingAttributes = inkCanvasMeasure.DefaultDrawingAttributes.Clone(),
//            };
//            inkCanvasMeasure.Strokes.Add(stroke);
//            //Calculate angle
//            double a = Math.Sqrt((iniP.X - line1P.X) * (iniP.X - line1P.X) + (iniP.Y - line1P.Y) * (iniP.Y - line1P.Y));
//            double b = Math.Sqrt((iniP.X - line2P.X) * (iniP.X - line2P.X) + (iniP.Y - line2P.Y) * (iniP.Y - line2P.Y));
//            double c = Math.Sqrt((line1P.X - line2P.X) * (line1P.X - line2P.X) + (line1P.Y - line2P.Y) * (line1P.Y - line2P.Y));
//            double cTheta = (a * a + b * b - c * c) / (2 * a * b);
//            double theta = Math.Acos(cTheta) * 180 / Math.PI;
//            //Draw Circle
//            double r = 25;
//            double rMax = a;
//            if (rMax > b)
//            {
//                rMax = b;
//            }
//            if (r > 0.5 * rMax)
//            {
//                r = 0.5 * rMax;
//            }
//            double theta0 = Math.Atan((iniP.Y - line1P.Y) / (line1P.X - iniP.X + 1e-10)) * 180 / Math.PI;
//            pointList = new List<System.Windows.Point>();
//            double cos_ab = ((line1P.Y - iniP.Y) * (line2P.Y - iniP.Y) + (line1P.X - iniP.X) * (line2P.X - iniP.X)) / (a * b);
//            double sin_ab = ((line1P.X - iniP.X) * (line2P.Y - iniP.Y) - (line1P.Y - iniP.Y) * (line2P.X - iniP.X)) / (a * b);
//            if (sin_ab <= 0)
//            {
//                for (double delta = 0; delta <= theta; delta++)
//                {
//                    double th = delta + theta0;
//                    pointList.Add(new System.Windows.Point(iniP.X + r * Math.Cos(th * Math.PI / 180), iniP.Y - r * Math.Sin(th * Math.PI / 180)));
//                }
//            }
//            else
//            {
//                for (double delta = -theta; delta <= 0; delta++)
//                {
//                    double th = delta + theta0;
//                    pointList.Add(new System.Windows.Point(iniP.X + r * Math.Cos(th * Math.PI / 180), iniP.Y - r * Math.Sin(th * Math.PI / 180)));
//                }
//            }
//            point = new StylusPointCollection(pointList);
//            stroke = new Stroke(point)
//            {
//                DrawingAttributes = inkCanvasMeasure.DefaultDrawingAttributes.Clone(),
//            };
//            inkCanvasMeasure.Strokes.Add(stroke);
//            viewModel.MeaInfo = "Current angle: " + string.Format("{0:F}", theta) + "°.";
//        }

//        private void InkCanvasMeasure_MouseDown(object sender, MouseButtonEventArgs e)
//        {
//            if (imgMeasure.Source == null)
//            {
//                return;
//            }
//            if (e.LeftButton == MouseButtonState.Pressed)
//            {
//                inkCanvasMeasure.Strokes.Clear();
//                viewModel.MeaInfo = "";
//                iniP = e.GetPosition(inkCanvasMeasure);
//            }
//            else if (e.RightButton == MouseButtonState.Pressed)
//            {
//                if (inkCanvasMeasure.Strokes.Count == 0)
//                {
//                    return;
//                }
//                while (inkCanvasMeasure.Strokes.Count > 1)
//                {
//                    inkCanvasMeasure.Strokes.RemoveAt(1);
//                }
//                line2P = e.GetPosition(inkCanvasMeasure);
//                DrawAxiesCircle();
//            }
//        }

//        private void InkCanvasMeasure_MouseMove(object sender, MouseEventArgs e)
//        {
//            if (imgMeasure.Source == null)
//            {
//                return;
//            }
//            if (e.LeftButton == MouseButtonState.Pressed)
//            {
//                // Measure Angle
//                inkCanvasMeasure.Strokes.Clear();
//                viewModel.MeaInfo = "";
//                line1P = e.GetPosition(inkCanvasMeasure);
//                if (line1P.X < iniP.X)
//                {
//                    return;
//                }
//                // Draw Line
//                List<System.Windows.Point> pointList = new List<System.Windows.Point>
//                        {
//                            new System.Windows.Point(iniP.X, iniP.Y),
//                            new System.Windows.Point(line1P.X, line1P.Y),
//                        };
//                StylusPointCollection point = new StylusPointCollection(pointList);
//                Stroke stroke = new Stroke(point)
//                {
//                    DrawingAttributes = inkCanvasMeasure.DefaultDrawingAttributes.Clone(),
//                };
//                inkCanvasMeasure.Strokes.Insert(0, stroke);
//            }
//            else if (e.RightButton == MouseButtonState.Pressed)
//            {
//                if (inkCanvasMeasure.Strokes.Count == 0)
//                {
//                    return;
//                }
//                while (inkCanvasMeasure.Strokes.Count > 1)
//                {
//                    inkCanvasMeasure.Strokes.RemoveAt(1);
//                }
//                line2P = e.GetPosition(inkCanvasMeasure);
//                DrawAxiesCircle();
//            }
//        }

//        //private void OpenFile_Click(object sender, RoutedEventArgs e)
//        //{
//        //    OpenFileDialog openDialog = new OpenFileDialog
//        //    {
//        //        Filter = "Image Files (*.jpg)|*.jpg|Image Files (*.png)|*.png|Image Files (*.bmp)|*.bmp",
//        //        Title = "Open Image File"
//        //    };
//        //    if (openDialog.ShowDialog() == true)
//        //    {
//        //        BitmapImage image = new BitmapImage();
//        //        image.BeginInit();
//        //        image.UriSource = new Uri(openDialog.FileName, UriKind.RelativeOrAbsolute);
//        //        image.EndInit();
//        //        imgMeasure.Source = image;
//        //    }
//        //}

//    }
//}