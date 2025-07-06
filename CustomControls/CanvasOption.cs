using wpf522.Models;
using wpf522.Models.DrawShapes;
using wpf522.Models.Enums;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Emgu.CV.OCR;
using wpf522.Adorners;
using System.ComponentModel;
using HandyControl.Controls;
using System.Windows.Media.TextFormatting;
using System.Collections.ObjectModel;
using System.IO;
using SharpVectors.Converters;
using static wpf522.SAMSegWindow;

namespace wpf522.CustomControls
{



    public partial class CanvasOption : MetroContentControl
    {



        public static readonly DependencyProperty CurrentDrawTypeProperty = DependencyProperty.Register("CurrentDrawType", typeof(ShapeType), typeof(CanvasOption), new PropertyMetadata(ShapeType.None));



        public ImageContentInfoModel? ImageModel => this.DataContext as ImageContentInfoModel;






        public bool isMeasure = false;


        public bool IsColorPicking { get; set; } = false;



        public List<TextBlock> measureList = new List<TextBlock>();

        private ObservableCollection<MeasureData> MeasureDataCollection;
        private Point initialPoint;  
        private bool isDrawingColorBox = false;

        public ShapeType CurrentDrawType
        {
            get => (ShapeType)GetValue(CurrentDrawTypeProperty);
            set => SetValue(CurrentDrawTypeProperty, value);
        }

        private void CanvasOption_Loaded(object sender, RoutedEventArgs e)
        {
            DrawImageBox.SizeChanged += DrawImageBox_SizeChanged;
            this.DrawCanvas.MouseDown += DrawCanvas_MouseDown;
            this.DrawCanvas.MouseMove += DrawCanvas_MouseMove;
            this.DrawCanvas.MouseUp += DrawCanvas_MouseUp;


        }



        public CanvasOption()
        {
            InitializeComponent();

            this.Loaded += CanvasOption_Loaded;
        }








        private void DrawImageBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ScaleStartPointProperty =
        DependencyProperty.Register("ScaleStartPoint", typeof(Point), typeof(CanvasOption), new PropertyMetadata(new Point(10, 10)));

        public Point ScaleStartPoint
        {
            get { return (Point)GetValue(ScaleStartPointProperty); }
            set { SetValue(ScaleStartPointProperty, value); }
        }

        public static readonly DependencyProperty ScaleEndPointProperty =
            DependencyProperty.Register("ScaleEndPoint", typeof(Point), typeof(CanvasOption), new PropertyMetadata(new Point(60, 10)));

        public Point ScaleEndPoint
        {
            get { return (Point)GetValue(ScaleEndPointProperty); }
            set { SetValue(ScaleEndPointProperty, value); }
        }

        public static readonly DependencyProperty ScaleTextProperty =
            DependencyProperty.Register("ScaleText", typeof(string), typeof(CanvasOption), new PropertyMetadata("1 cm"));

        public string ScaleText
        {
            get { return (string)GetValue(ScaleTextProperty); }
            set { SetValue(ScaleTextProperty, value); }
        }

        public static readonly DependencyProperty ScaleVisibilityProperty =
            DependencyProperty.Register("ScaleVisibility", typeof(Visibility), typeof(CanvasOption), new PropertyMetadata(Visibility.Visible));

        public Visibility ScaleVisibility
        {
            get { return (Visibility)GetValue(ScaleVisibilityProperty); }
            set { SetValue(ScaleVisibilityProperty, value); }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void StartAngleDrawing_Click(object sender, RoutedEventArgs e)
        {
            isDrawingAngle = true;
        }




        private Point? _rulerStartPoint = null;
        private Line _rulerLine;
        private TextBlock _rulerText;
        private Ellipse _startPointEllipse, _endPointEllipse;
        private double _pixelToRealRatio = 1.0; 
        public event Action<double> SaveArea;
        public ObservableCollection<LabelItem> Labels { get; set; } = new ObservableCollection<LabelItem>();
        public LabelItem SelectedLabel { get; set; }

        public ObservableCollection<DataModel> DataCollection { get; set; } = new ObservableCollection<DataModel>();

        private int _currentId = 1; 

        private string _unit = "cm"; 
        private string _currentFileName;


        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDrawType = ShapeType.ColorPicker;  
            IsDrawBox = false;  
        }
        public void StartColorDrawing()
        {
            isDrawingColorBox = true;  
        }


        #region -----·½·¨-----

        private List<Line> PolygonShape = new List<Line>();
        private Rectangle RectangleShape = new Rectangle();
        private Line CurrentLin;
        private bool IsDrawPolygon = false;
        private bool IsDrawBox = false;
        private bool IsDrawLines = false;

        private Point firstPoint;
        private Point secondPoint;
        private Point thirdPoint;
        private Point fourthPoint;
        private Line firstLine;
        private Line secondLine;
        private Line tempLine1; 
        private bool isDrawingFirstLine = false;
        private bool isDrawingSecondLine = false;
        private bool isDrawingAngle = false;

        private Polygon polygonMask;
        private Shape selectedShape = null;

        public class MeasureData
        {
            public string Label { get; set; }
            public double Length { get; set; }
            public double Area { get; set; }
            public double Angle { get; set; }
        }

        public void DeleteAngle()
        {
            if (firstLine != null) DrawCanvas.Children.Remove(firstLine);
            if (secondLine != null) DrawCanvas.Children.Remove(secondLine);
            if (tempLine1 != null) DrawCanvas.Children.Remove(tempLine1);


            isDrawingFirstLine = false;
            isDrawingSecondLine = false;
            firstLine = null;
            secondLine = null;
            tempLine1 = null;

        }



        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {


            if (this.ImageModel is null || e.ChangedButton != MouseButton.Left || CurrentDrawType == ShapeType.None) return;
            var point = e.GetPosition(DrawCanvas);







            Ellipse glowEllipse = new Ellipse();
            glowEllipse.Width = 20;
            glowEllipse.Height = 20;
            glowEllipse.Stroke = Brushes.LightBlue;
            glowEllipse.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
            glowEllipse.Margin = new Thickness(point.X - 10, point.Y - 10, 0, 0); 
            DrawCanvas.Children.Add(glowEllipse);

            DoubleAnimation animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2)); 
            Storyboard.SetTarget(animation, glowEllipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
            storyboard.Completed += (s, _) =>
            {
                DrawCanvas.Children.Remove(glowEllipse); 
            };

            if (CurrentDrawType == ShapeType.Polygon)
            {
                HandlePolygonMouseDown(point);
            }

            if (CurrentDrawType == ShapeType.None) return;
            if (CurrentDrawType == ShapeType.Polygon && IsDrawPolygon is false)
            {

                CurrentLin = new Line();
                CurrentLin.X1 = point.X;
                CurrentLin.Y1 = point.Y;
                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
                CurrentLin.Stroke = Brushes.SteelBlue;
                CurrentLin.StrokeThickness = 1.8;
                DrawCanvas.Children.Add(CurrentLin);
                PolygonShape.Add(CurrentLin);

                polygonMask = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromArgb(10, 0, 0, 255)), 
                    Stroke = Brushes.Transparent,
                    StrokeThickness = 0
                };
                DrawCanvas.Children.Add(polygonMask);
                InitializePolygonMask();
                UpdatePolygonMask();


                IsDrawPolygon = true;
            }

            else if (CurrentDrawType == ShapeType.Polygon && IsDrawPolygon)
            {

                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
                CurrentLin = new Line();
                CurrentLin.X1 = point.X;
                CurrentLin.Y1 = point.Y;
                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
                CurrentLin.Stroke = Brushes.SteelBlue;
                CurrentLin.StrokeThickness = 2;
                DrawCanvas.Children.Add(CurrentLin);
                PolygonShape.Add(CurrentLin);

                UpdatePolygonMask();

                if (IsPolygonClosed(point))
                {
                    CompletePolygon();
                }

            }


            else if (CurrentDrawType == ShapeType.Box && !IsDrawBox)
            {

                DrawCanvas.Children.Add(RectangleShape);
                Canvas.SetLeft(RectangleShape, point.X);
                Canvas.SetTop(RectangleShape, point.Y);
                RectangleShape.StrokeThickness = 2;
                RectangleShape.Stroke = Brushes.SteelBlue;
                IsDrawBox = true;

            }

            else if (CurrentDrawType == ShapeType.ColorPicker && !isDrawingColorBox)
            {

                RectangleShape = new Rectangle
                {
                    Width = 0,
                    Height = 0,
                    StrokeThickness = 2,
                    Stroke = Brushes.SteelBlue
                };

                DrawCanvas.Children.Add(RectangleShape);
                Canvas.SetLeft(RectangleShape, point.X);
                Canvas.SetTop(RectangleShape, point.Y);

                isDrawingColorBox = true;
                initialPoint = point;  







            }



            else if (CurrentDrawType == ShapeType.Angle) 
            {
                var apoint = e.GetPosition(DrawCanvas);
                if (e.ChangedButton == MouseButton.Right)
                {

                    DeleteAngle();
                    return;
                }
                if (CurrentDrawType == ShapeType.Angle)
                {
                    if (!isDrawingFirstLine && !isDrawingSecondLine)
                    {

                        firstPoint = point;
                        firstLine = new Line
                        {
                            X1 = point.X,
                            Y1 = point.Y,
                            X2 = point.X,
                            Y2 = point.Y,
                            Stroke = Brushes.DarkTurquoise,
                            StrokeThickness = 1
                        };
                        DrawCanvas.Children.Add(firstLine);
                        isDrawingFirstLine = true;
                    }
                    else if (isDrawingFirstLine && !isDrawingSecondLine)
                    {

                        secondPoint = point;
                        firstLine.X2 = point.X;
                        firstLine.Y2 = point.Y;

                        tempLine1 = new Line
                        {
                            X1 = point.X,
                            Y1 = point.Y,
                            X2 = point.X + (point.X - firstPoint.X),
                            Y2 = point.Y + (point.Y - firstPoint.Y),
                            Stroke = Brushes.Gray,
                            StrokeThickness = 1,
                            StrokeDashArray = new DoubleCollection { 2, 2 }
                        };
                        DrawCanvas.Children.Add(tempLine1);
                        isDrawingFirstLine = false;
                        isDrawingSecondLine = true;
                    }
                    else if (isDrawingSecondLine)
                    {

                        thirdPoint = secondPoint; 
                        fourthPoint = point; 
                        secondLine = new Line
                        {
                            X1 = thirdPoint.X,
                            Y1 = thirdPoint.Y,
                            X2 = point.X,
                            Y2 = point.Y,
                            Stroke = Brushes.DarkTurquoise,
                            StrokeThickness = 1
                        };
                        DrawCanvas.Children.Add(secondLine);

                        double angle = CalculateAngle(firstPoint, secondPoint, thirdPoint, fourthPoint);
                        ShowAngle(secondPoint, angle);

                        isDrawingSecondLine = false;

                    }
                }
            }

            else if (CurrentDrawType == ShapeType.Lines)
            {
                Point startPoint = e.GetPosition(DrawCanvas);

                Line newLine = new Line();
                newLine.X1 = point.X;
                newLine.Y1 = point.Y;
                newLine.X2 = point.X;
                newLine.Y2 = point.Y;
                newLine.Stroke = Brushes.SteelBlue;
                newLine.StrokeThickness = 2;
                DrawCanvas.Children.Add(newLine);

                LinesAdorner adorner = new LinesAdorner(newLine);
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(DrawCanvas);
                if (adornerLayer != null)
                {
                    adornerLayer.Add(adorner);
                }
                else
                {
                    Console.WriteLine("AdornerLayer is null");
                }

                CreateMeasure(point);

                CurrentLin = newLine; 
                Console.WriteLine("Draw line at: ({0},{1}) to ({2},{3})", point.X, point.Y, point.X, point.Y);
                IsDrawLines = true;

            }
        }

        private void InitializePolygonMask()
        {
            if (polygonMask != null)
            {
                DrawCanvas.Children.Remove(polygonMask);
            }

            polygonMask = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(51, 0, 0, 255)), 
                Stroke = Brushes.Transparent,
                StrokeThickness = 0
            };
            DrawCanvas.Children.Add(polygonMask);
        }
        private void UpdatePolygonMask()
        {
            if (polygonMask == null)
            {
                InitializePolygonMask();
            }

            var points = new PointCollection();
            foreach (var line in PolygonShape)
            {
                points.Add(new Point(line.X1, line.Y1));
            }
            if (PolygonShape.Count > 0)
            {
                points.Add(new Point(PolygonShape[0].X1, PolygonShape[0].Y1)); 
            }
            polygonMask.Points = points;
        }

        private void CompletePolygon()
        {

            List<Point> polygonPoints = new List<Point>();
            foreach (var line in PolygonShape)
            {
                polygonPoints.Add(new Point(line.X1, line.Y1));
            }

            double area = CalculatePolygonArea(polygonPoints);

            ShowArea(polygonPoints, area);

            PolygonShape.Clear();
            IsDrawPolygon = false;
        }

        private void ShowArea(List<Point> points, double area)
        {
            if (points.Count == 0) return;

            double centerX = 0, centerY = 0;
            foreach (var point in points)
            {
                centerX += point.X;
                centerY += point.Y;
            }
            centerX /= points.Count;
            centerY /= points.Count;

            TextBlock areaText = new TextBlock
            {
                Text = $"Area: {area:F2} units2",
                FontSize = 14,
                Foreground = Brushes.Bisque,
            };
            Canvas.SetLeft(areaText, centerX);
            Canvas.SetTop(areaText, centerY);
            DrawCanvas.Children.Add(areaText);
        }

        private bool IsPolygonClosed(Point point)
        {
            if (PolygonShape.Count < 3) return false;
            Point firstPoint = new Point(PolygonShape[0].X1, PolygonShape[0].Y1);
            return Math.Abs(point.X - firstPoint.X) < 10 && Math.Abs(point.Y - firstPoint.Y) < 10;
        }


        private double CalculatePolygonArea(List<Point> points)
        {
            int pointCount = points.Count;
            if (pointCount < 3) return 0;

            double area = 0;
            for (int i = 0; i < pointCount; i++)
            {
                int j = (i + 1) % pointCount;
                area += points[i].X * points[j].Y;
                area -= points[i].Y * points[j].X;
            }


            area = Math.Abs(area) / 2.0;
            return area;
        }

        private double CalculatePolygonPerimeter(List<Point> points)
        {
            double perimeter = 0;
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i + 1) % points.Count;
                perimeter += DistanceBetween(points[i], points[j]);
            }
            return perimeter;
        }

        private double DistanceBetween(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        private void HandlePolygonMouseDown(Point point)
        {
            if (!IsDrawPolygon)
            {

                CurrentLin = new Line
                {
                    X1 = point.X,
                    Y1 = point.Y,
                    X2 = point.X,
                    Y2 = point.Y,
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 2
                };
                DrawCanvas.Children.Add(CurrentLin);
                PolygonShape.Add(CurrentLin);
                IsDrawPolygon = true;
            }
            else
            {

                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;

                CurrentLin = new Line
                {
                    X1 = point.X,
                    Y1 = point.Y,
                    X2 = point.X,
                    Y2 = point.Y,
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 2
                };
                DrawCanvas.Children.Add(CurrentLin);
                PolygonShape.Add(CurrentLin);
            }
        }

        private double CalculateAngle(Point pt1, Point pt2, Point pt3, Point pt4)
        {

            double dx1 = pt2.X - pt1.X;
            double dy1 = pt2.Y - pt1.Y;

            double dx2 = pt4.X - pt3.X;
            double dy2 = pt4.Y - pt3.Y;

            double dotProduct = (dx1 * dx2) + (dy1 * dy2);
            double magnitude1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            double magnitude2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            double angle = Math.Acos(dotProduct / (magnitude1 * magnitude2)) * 180.0 / Math.PI;
            angle = 180 - angle;

            return angle;
        }

        private void ShowAngle(Point position, double angle)
        {
            TextBlock angleText = new TextBlock
            {
                Text = $"{angle:0.00}¡ã",
                FontSize = 15,
                Foreground = Brushes.DarkTurquoise,

            };
            Canvas.SetLeft(angleText, position.X);
            Canvas.SetTop(angleText, position.Y);
            DrawCanvas.Children.Add(angleText);
        }





        private void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.ImageModel is null) return; 
            if (e.ChangedButton == MouseButton.Left) 
            {
                var point = e.GetPosition(DrawCanvas); 

                if (ToolConfig.IsColorPicking)
                {
                    WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);
                    var color = GetColorAtPixel((int)point.X, (int)point.Y, writeableBitmap);
                    ShowColorResultWindow(color);



                    return;
                }
                if (IsDrawPolygon)
                {
                    CurrentLin.X2 = point.X;
                    CurrentLin.Y2 = point.Y;
                }
                else if (IsDrawLines)
                {
                    if (this.ImageModel is null) return; 
                    if (e.ChangedButton == MouseButton.Left && IsDrawLines) 
                    {
                        Point endPoint = e.GetPosition(DrawCanvas);

                        CurrentLin.X2 = point.X;
                        CurrentLin.Y2 = point.Y;

                        MeasureDistance();

                        CurrentLin = null;
                        IsDrawLines = false;
                    }
                }

                else if (CurrentDrawType == ShapeType.Box && IsDrawBox)
                {

                    var box = new ShapeBox()
                    {
                        X = Canvas.GetLeft(RectangleShape),
                        Y = Canvas.GetTop(RectangleShape),
                        Width = RectangleShape.Width,
                        Height = RectangleShape.Height,
                        TypeName = MainWindow.Instance.MainModel.ToolConfig.GetCurrentTypeName().TypeName,
                    };

                    if (box.Width * box.Height > 15)
                    {
                        ImageModel.Shapes.Add(box);
                    }

                    DrawCanvas.Children.Remove(RectangleShape);
                    IsDrawBox = false;
                    RectangleShape.Width = 1;
                    RectangleShape.Height = 1;
                }
            }
            else if (CurrentDrawType == ShapeType.ColorPicker && isDrawingColorBox)
            {

                var boxRect = new Rect(Canvas.GetLeft(RectangleShape), Canvas.GetTop(RectangleShape), RectangleShape.Width, RectangleShape.Height);
                var avgColor = GetAverageColorInRectangle(boxRect);
                var point = e.GetPosition(DrawCanvas); 







                ShowColorResultWindow(avgColor);

                DrawCanvas.Children.Remove(RectangleShape);
                isDrawingColorBox = false;
            }

            else if (e.ChangedButton == MouseButton.Right)
            {

                if (IsDrawPolygon)
                {
                    IsDrawPolygon = false;
                    CurrentLin.X2 = this.PolygonShape.First().X1;
                    CurrentLin.Y2 = this.PolygonShape.First().Y1;
                    if (PolygonShape.Count > 1)
                    {
                        var res = ConvertToShapePolygon();
                        this.ImageModel.Shapes.Add(res);
                    }
                    ClearPolygonLines();
                    IsDrawPolygon = false;
                }
                TextBlock txtArea = new TextBlock();
                txtArea.FontSize = 18;
                txtArea.Foreground = Brushes.SteelBlue;
                txtArea.Height = 30;
                txtArea.Width = 200;
                Canvas.SetLeft(txtArea, 10); 
                Canvas.SetTop(txtArea, 10);
                DrawCanvas.Children.Add(txtArea); 


            }

        }





        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (this.ImageModel is null) return;
            if (!IsDrawBox && !IsDrawPolygon) return;
            var point = e.GetPosition(DrawCanvas);
            if (IsDrawPolygon)
            {

                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
            }
            else if (CurrentDrawType == ShapeType.Angle)
            {

                if (isDrawingFirstLine)
                {
                    firstLine.X2 = point.X;
                    firstLine.Y2 = point.Y;

                    if (tempLine1 != null)
                    {
                        tempLine1.X2 = point.X + (point.X - firstLine.X1);
                        tempLine1.Y2 = point.Y + (point.Y - firstLine.Y1);
                    }
                }

                else if (isDrawingSecondLine)
                {
                    secondLine.X2 = point.X;
                    secondLine.Y2 = point.Y;
                }

            }
            else if (CurrentDrawType == ShapeType.Box && IsDrawBox)
            {

                var p = new Point(Canvas.GetLeft(RectangleShape), Canvas.GetTop(RectangleShape));
                double x = Math.Min(point.X, p.X);
                double y = Math.Min(point.Y, p.Y);

                RectangleShape.Width = Math.Abs(point.X - p.X);
                RectangleShape.Height = Math.Abs(point.Y - p.Y);
                Canvas.SetLeft(RectangleShape, x);
                Canvas.SetTop(RectangleShape, y);
            }
            else if (CurrentDrawType == ShapeType.ColorPicker && isDrawingColorBox)
            {

                double width = point.X - initialPoint.X;
                double height = point.Y - initialPoint.Y;

                RectangleShape.Width = Math.Abs(width);
                RectangleShape.Height = Math.Abs(height);

                Canvas.SetLeft(RectangleShape, Math.Min(point.X, initialPoint.X));
                Canvas.SetTop(RectangleShape, Math.Min(point.Y, initialPoint.Y));
            }

            else if (CurrentDrawType == ShapeType.Lines)
            {
                Point movePoint = e.GetPosition(DrawCanvas);  

                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;

            }

        }

        TextBlock txtMeasure;






        public void CreateMeasure(Point point)
        {





            TextBlock txtMeasure = new TextBlock();
            txtMeasure.Text = "mm";
            txtMeasure.FontSize = 14;
            txtMeasure.Foreground = Brushes.DarkBlue;
            txtMeasure.Height = 30;
            txtMeasure.Width = 100;
            Canvas.SetLeft(txtMeasure, point.X);
            Canvas.SetTop(txtMeasure, point.Y);
            DrawCanvas.Children.Add(txtMeasure);
            measureList.Add(txtMeasure);
        }

        public void MeasureDistance()
        {
            if (CurrentDrawType == ShapeType.Lines && measureList.Count > 0)
            {
                Point p1 = new Point(CurrentLin.X1, CurrentLin.Y1);
                Point p2 = new Point(CurrentLin.X2, CurrentLin.Y2);
                double distance = GetDistance(p1, p2, 1.0, 1.0); 
                TextBlock txtMeasure = measureList[measureList.Count - 1];
                txtMeasure.Text = $"{distance.ToString("0.000")}mm";

                double centerX = (CurrentLin.X1 + CurrentLin.X2) / 2;
                double centerY = (CurrentLin.Y1 + CurrentLin.Y2) / 2;

                Canvas.SetLeft(txtMeasure, centerX);
                Canvas.SetTop(txtMeasure, centerY);
            }
        }









        public static double GetDistance(Point p1, Point p2, double dpiX, double dpiY)
        {
            double result = 0;
            result = Math.Sqrt((p1.X * dpiX - p2.X * dpiX) * (p1.X * dpiX - p2.X * dpiX) +
                               (p1.Y * dpiY - p2.Y * dpiY) * (p1.Y * dpiY - p2.Y * dpiY));
            return result;
        }

        private Point DisplayToImageCoords(Point displayPoint)
        {
            if (DrawImageBox.RenderTransform is TransformGroup transformGroup &&
                transformGroup.Children[0] is ScaleTransform scaleTransform)
            {
                double scale = scaleTransform.ScaleX; 

                double xInOriginal = displayPoint.X / scale;
                double yInOriginal = displayPoint.Y / scale;

                return new Point(xInOriginal, yInOriginal);
            }

            return displayPoint; 
        }





































































































        private void ResetRuler()
        {
            _rulerStartPoint = null;

            if (_rulerLine != null) DrawCanvas.Children.Remove(_rulerLine);
            if (_startPointEllipse != null) DrawCanvas.Children.Remove(_startPointEllipse);
            if (_endPointEllipse != null) DrawCanvas.Children.Remove(_endPointEllipse);
        }






        private void DrawCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.ImageModel is null) return;
            if (IsDrawPolygon)
            {  
                this.DrawCanvas.Children.Remove(CurrentLin);
                this.PolygonShape.Remove(CurrentLin);
                ClearPolygonLines();
                IsDrawPolygon = false;
            }
            else if (CurrentDrawType == ShapeType.Box && IsDrawBox)
            {
                DrawCanvas.Children.Remove(RectangleShape);
                IsDrawBox = false;
            }

            else if (CurrentDrawType == ShapeType.Lines && IsDrawLines)
            {
                DrawCanvas.Children.Remove(CurrentLin);
                if (DrawCanvas.Children.Contains(CurrentLin))
                {
                    DrawCanvas.Children.Remove(CurrentLin);
                }
                IsDrawLines = false;
            }
        }




        private ShapePolygon ConvertToShapePolygon()
        {
            var x = double.MaxValue;
            var y = double.MaxValue;
            foreach (var item in PolygonShape)
            {
                if (item.X1 < x)
                {
                    x = item.X1;
                }
                if (item.Y1 < y)
                {
                    y = item.Y1;
                }
            }

            ShapePolygon shapePolygon = new ShapePolygon();
            shapePolygon.StartX = x;
            shapePolygon.StartY = y;
            shapePolygon.TypeName = MainWindow.Instance.MainModel.ToolConfig.GetCurrentTypeName().TypeName;
            foreach (var item in PolygonShape)
            {
                shapePolygon.Points.Add(new Point(item.X1 - x, item.Y1 - y));
            }

            return shapePolygon;
        }



        private void ClearPolygonLines()
        {
            foreach (var item in PolygonShape)
            {
                this.DrawCanvas.Children.Remove(item);
            }
            PolygonShape.Clear();
        }

        private WriteableBitmap GetWriteableBitmapFromImageSource(Image image)
        {
            if (image.Source is BitmapSource bitmapSource)
            {
                return new WriteableBitmap(bitmapSource);
            }
            return null;
        }

        private Color GetColorAtPixel(int x, int y, WriteableBitmap writeableBitmap)
        {
            if (x < 0 || y < 0 || x >= writeableBitmap.PixelWidth || y >= writeableBitmap.PixelHeight)
            {
                return Colors.Transparent;  
            }

            int stride = writeableBitmap.PixelWidth * 4;  
            byte[] pixels = new byte[stride * writeableBitmap.PixelHeight];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            int pixelIndex = (y * stride) + (x * 4);  

            byte b = pixels[pixelIndex];     
            byte g = pixels[pixelIndex + 1]; 
            byte r = pixels[pixelIndex + 2]; 
            byte a = pixels[pixelIndex + 3]; 

            return Color.FromArgb(a, r, g, b);
        }


        private Color GetAverageColorInRectangle(Rect rect)
        {

            WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

            int startX = (int)Math.Floor(rect.X);
            int startY = (int)Math.Floor(rect.Y);
            int width = (int)rect.Width;
            int height = (int)rect.Height;

            if (startX < 0) startX = 0;
            if (startY < 0) startY = 0;
            if (startX + width > writeableBitmap.PixelWidth) width = writeableBitmap.PixelWidth - startX;
            if (startY + height > writeableBitmap.PixelHeight) height = writeableBitmap.PixelHeight - startY;

            long sumR = 0, sumG = 0, sumB = 0;
            long pixelCount = 0;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    var color = GetColorAtPixel(x, y, writeableBitmap);  
                    sumR += color.R;
                    sumG += color.G;
                    sumB += color.B;
                    pixelCount++;
                }
            }

            byte avgR = (byte)(sumR / pixelCount);
            byte avgG = (byte)(sumG / pixelCount);
            byte avgB = (byte)(sumB / pixelCount);

            return Color.FromRgb(avgR, avgG, avgB);
        }


        private void ShowColorResultWindow(Color avgColor)
        {
            var resultWindow = new System.Windows.Window
            {
                Title = "Average Color",
                Width = 250,  
                Height = 150, 
                ResizeMode = ResizeMode.NoResize, 
                WindowStartupLocation = WindowStartupLocation.Manual 
            };

            var stackPanel = new StackPanel
            {

                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                Text = $"RGB: {avgColor.R}, {avgColor.G}, {avgColor.B}\nHex: {ColorToHex(avgColor)}",
                FontSize = 14,
                Foreground = Brushes.Black
            };

            var colorBlock = new Rectangle
            {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(avgColor),
                Margin = new Thickness(0, 5, 0, 0) 
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(colorBlock);

            resultWindow.Content = stackPanel;

            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow); 
            var screenPosition = Application.Current.MainWindow.PointToScreen(mousePosition); 
            resultWindow.Left = screenPosition.X + 10; 
            resultWindow.Top = screenPosition.Y + 10;

            resultWindow.ShowDialog();
        }

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }



    }

}


#endregion

