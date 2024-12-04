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

namespace wpf522.CustomControls
{
    /// <summary>
    /// CanvasOption.xaml 的交互逻辑
    /// </summary>
    public partial class CanvasOption : MetroContentControl
    {
        /// <summary>
        /// 当前绘制状态
        /// </summary>
        public static readonly DependencyProperty CurrentDrawTypeProperty = DependencyProperty.Register("CurrentDrawType", typeof(ShapeType), typeof(CanvasOption), new PropertyMetadata(ShapeType.None));

        /// <summary>
        /// 模型
        /// </summary>
        public ImageContentInfoModel? ImageModel => this.DataContext as ImageContentInfoModel;
        /// <summary>
        /// 当前操作的类型
        /// </summary>
        /// /// <summary>
        /// 是否为测量行为
        /// </summary>
        public bool isMeasure = false;

        /// <summary>
        /// 测量对象集合
        /// </summary>
        public List<TextBlock> measureList = new List<TextBlock>();

        private ObservableCollection<MeasureData> MeasureDataCollection;
        private Point initialPoint;  // 记录初始点位置
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
        /// <summary>
        /// 构造函数
        /// </summary>
        public CanvasOption()
        {
            InitializeComponent();

            this.Loaded += CanvasOption_Loaded;
        }

        /// <summary>
        /// 尺寸变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// 

        private void DrawImageBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 定义标尺的依赖属性
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

        // 点击 "Color" 按钮，切换绘制模式
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDrawType = ShapeType.ColorPicker;  // 设置当前绘制类型为 "ColorPicker"
            IsDrawBox = false;  // 确保只允许绘制一个框
        }
        public void StartColorDrawing()
        {
            isDrawingColorBox = true;  // 启动绘制颜色框
        }


        #region -----方法-----
        // 鼠标控制
        private List<Line> PolygonShape = new List<Line>();
        private Rectangle RectangleShape = new Rectangle();
        private Line CurrentLin;
        private bool IsDrawPolygon = false;
        private bool IsDrawBox = false;
        private bool IsDrawLines = false;
        //角度 
        private Point firstPoint;
        private Point secondPoint;
        private Point thirdPoint;
        private Point fourthPoint;
        private Line firstLine;
        private Line secondLine;
        private Line tempLine1; // 第一条线的延长线
        private bool isDrawingFirstLine = false;
        private bool isDrawingSecondLine = false;
        private bool isDrawingAngle = false;
       

        // 多边形掩膜
        private Polygon polygonMask;
        private Shape selectedShape = null;
      

        //定义存储测量数据的类
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
            //if (angleText != null) DrawCanvas.Children.Remove(angleText);

            // 重置状态
            isDrawingFirstLine = false;
            isDrawingSecondLine = false;
            firstLine = null;
            secondLine = null;
            tempLine1 = null;
            //angleText = null;
        }

        /// <summary>
        ///  发起绘制（MouseDown）按下
        /// </summary>
        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {


            if (this.ImageModel is null || e.ChangedButton != MouseButton.Left || CurrentDrawType == ShapeType.None) return;
            var point = e.GetPosition(DrawCanvas);
            // 获取当前图像的 WriteableBitmap
            WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

            // 获取鼠标点击位置的颜色
            var color = GetColorAtPixel((int)point.X, (int)point.Y, writeableBitmap);

            // 在这里可以处理获取的颜色，例如显示颜色信息
            ShowColorResultWindow(color);
            // 配置荧光球动画
            Ellipse glowEllipse = new Ellipse();
            glowEllipse.Width = 20;
            glowEllipse.Height = 20;
            glowEllipse.Stroke = Brushes.LightBlue;
            glowEllipse.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
            glowEllipse.Margin = new Thickness(point.X - 10, point.Y - 10, 0, 0); // 定位中心点
            DrawCanvas.Children.Add(glowEllipse);

            DoubleAnimation animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2)); // 0.2 秒内从不透明到透明
            Storyboard.SetTarget(animation, glowEllipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
            storyboard.Completed += (s, _) =>
            {
                DrawCanvas.Children.Remove(glowEllipse); // 动画结束时移除球
            };

            if (CurrentDrawType == ShapeType.Polygon)
            {
                HandlePolygonMouseDown(point);
            }

            if (CurrentDrawType == ShapeType.None) return;
            if (CurrentDrawType == ShapeType.Polygon && IsDrawPolygon is false)
            {
                // 创建初始点
                CurrentLin = new Line();
                CurrentLin.X1 = point.X;
                CurrentLin.Y1 = point.Y;
                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
                CurrentLin.Stroke = Brushes.SteelBlue;
                CurrentLin.StrokeThickness = 1.8;
                DrawCanvas.Children.Add(CurrentLin);
                PolygonShape.Add(CurrentLin);

                // 初始化多边形掩膜
                polygonMask = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromArgb(10, 0, 0, 255)), // 20%透明度蓝色
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
                // 更新绘制线条的终点并创建新线段
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

                // 更新掩膜顶点
                UpdatePolygonMask();


                // 检查是否完成多边形绘制
                if (IsPolygonClosed(point))
                {
                    CompletePolygon();
                }

            }
            
        
            else if (CurrentDrawType == ShapeType.Box && !IsDrawBox)
            {
                // 绘制矩形框
                DrawCanvas.Children.Add(RectangleShape);
                Canvas.SetLeft(RectangleShape, point.X);
                Canvas.SetTop(RectangleShape, point.Y);
                RectangleShape.StrokeThickness = 2;
                RectangleShape.Stroke = Brushes.SteelBlue;
                IsDrawBox = true;

            }
            // 绘制颜色框
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

                // 开始绘制颜色框
                isDrawingColorBox = true;
                initialPoint = point;  // 记录起始点

                //var point = e.GetPosition(DrawCanvas);
                //// 获取当前图像的 WriteableBitmap
                //WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

                //// 获取鼠标点击位置的颜色
                //var color = GetColorAtPixel((int)point.X, (int)point.Y, writeableBitmap);

                //// 在这里可以处理获取的颜色，例如显示颜色信息
                //ShowColorResultWindow(color);
            }



            else if (CurrentDrawType == ShapeType.Angle) // 角度绘制
            {
                var apoint = e.GetPosition(DrawCanvas);
                if (e.ChangedButton == MouseButton.Right)
                {
                    // 右键点击，删除所有相关线条和文本
                    DeleteAngle();
                    return;
                }
                if (CurrentDrawType == ShapeType.Angle)
                {
                    if (!isDrawingFirstLine && !isDrawingSecondLine)
                    {
                        // 开始绘制第一条线
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
                        // 完成第一条线
                        secondPoint = point;
                        firstLine.X2 = point.X;
                        firstLine.Y2 = point.Y;

                        // 延长线
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
                        // 完成第二条线
                        thirdPoint = secondPoint; // 第三点是第一条线的终点
                        fourthPoint = point; // 第四点是第二条线的终点
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

                        // 计算角度并显示
                        double angle = CalculateAngle(firstPoint, secondPoint, thirdPoint, fourthPoint);
                        ShowAngle(secondPoint, angle);

                        // 重置绘制状态
                        isDrawingSecondLine = false;
                        //DrawCanvas.Children.Remove(tempLine1); // 移除辅助线
                    }
                }
            }

            else if (CurrentDrawType == ShapeType.Lines)
            {
                Point startPoint = e.GetPosition(DrawCanvas);

                // 绘制直线段
                Line newLine = new Line();
                newLine.X1 = point.X;
                newLine.Y1 = point.Y;
                newLine.X2 = point.X;
                newLine.Y2 = point.Y;
                newLine.Stroke = Brushes.SteelBlue;
                newLine.StrokeThickness = 2;
                DrawCanvas.Children.Add(newLine);

                // 添加 Adorner
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

                // 创建测量文本
                CreateMeasure(point);

                CurrentLin = newLine; // 赋值给 CurrentLin
                Console.WriteLine("Draw line at: ({0},{1}) to ({2},{3})", point.X, point.Y, point.X, point.Y);
                IsDrawLines = true;

            }
        }
        //多边形掩膜
        private void InitializePolygonMask()
        {
            if (polygonMask != null)
            {
                DrawCanvas.Children.Remove(polygonMask);
            }

            polygonMask = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(51, 0, 0, 255)), // 20%透明度蓝色
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
                points.Add(new Point(PolygonShape[0].X1, PolygonShape[0].Y1)); // 闭合多边形
            }
            polygonMask.Points = points;
        }

        private void CompletePolygon()
        {
            // 创建多边形的顶点列表
            List<Point> polygonPoints = new List<Point>();
            foreach (var line in PolygonShape)
            {
                polygonPoints.Add(new Point(line.X1, line.Y1));
            }

            // 计算面积
            double area = CalculatePolygonArea(polygonPoints);

            // 显示面积
            ShowArea(polygonPoints, area);

         



            // 清空多边形数据
            PolygonShape.Clear();
            IsDrawPolygon = false;
        }

        private void ShowArea(List<Point> points, double area)
        {
            if (points.Count == 0) return;

            // 计算多边形的中心点
            double centerX = 0, centerY = 0;
            foreach (var point in points)
            {
                centerX += point.X;
                centerY += point.Y;
            }
            centerX /= points.Count;
            centerY /= points.Count;

            // 显示面积值
            TextBlock areaText = new TextBlock
            {
                Text = $"Area: {area:F2} units²",
                FontSize = 14,
                Foreground = Brushes.Bisque,
            };
            Canvas.SetLeft(areaText, centerX);
            Canvas.SetTop(areaText, centerY);
            DrawCanvas.Children.Add(areaText);
        }
        //闭合情况检查，检查多边形是否至少有三个顶点。如果是，闭合多边形，计算面积
        private bool IsPolygonClosed(Point point)
        {
            if (PolygonShape.Count < 3) return false;
            Point firstPoint = new Point(PolygonShape[0].X1, PolygonShape[0].Y1);
            return Math.Abs(point.X - firstPoint.X) < 10 && Math.Abs(point.Y - firstPoint.Y) < 10;
        }

        //计算多边形面积

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
        // 计算多边形周长的方法
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

        // 计算两点间距离的方法
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
                // Initialize polygon drawing
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
                // Continue drawing polygon
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
        //计算角度
        private double CalculateAngle(Point pt1, Point pt2, Point pt3, Point pt4)
        {
            // 向量1
            double dx1 = pt2.X - pt1.X;
            double dy1 = pt2.Y - pt1.Y;

            // 向量2
            double dx2 = pt4.X - pt3.X;
            double dy2 = pt4.Y - pt3.Y;

            // 计算点乘和长度
            double dotProduct = (dx1 * dx2) + (dy1 * dy2);
            double magnitude1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            double magnitude2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            // 计算夹角
            double angle = Math.Acos(dotProduct / (magnitude1 * magnitude2)) * 180.0 / Math.PI;
            angle = 180 - angle;

            return angle;
        }

        // 显示角度
        private void ShowAngle(Point position, double angle)
        {
            TextBlock angleText = new TextBlock
            {
                Text = $"{angle:0.00}°",
                FontSize = 15,
                Foreground = Brushes.DarkTurquoise,
                // Background = Brushes.White
            };
            Canvas.SetLeft(angleText, position.X);
            Canvas.SetTop(angleText, position.Y);
            DrawCanvas.Children.Add(angleText);
        }
        /// <summary>
        /// 抬起 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.ImageModel is null) return; //检查 ImageModel 是否为 null，如果是 直接返回
            if (e.ChangedButton == MouseButton.Left) //检查鼠标按钮是否为左
            {
                var point = e.GetPosition(DrawCanvas); //获取鼠标相对 DrawCanvas 的位置。
                //                                       // 获取当前图像的 WriteableBitmap
                //WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

                //// 获取鼠标点击位置的颜色
                //var color = GetColorAtPixel((int)point.X, (int)point.Y, writeableBitmap);

                //// 在这里可以处理获取的颜色，例如显示颜色信息
                //ShowColorResultWindow(color);
                if (IsDrawPolygon)
                {
                    CurrentLin.X2 = point.X;
                    CurrentLin.Y2 = point.Y;
                }
                else if (IsDrawLines)
                {
                    if (this.ImageModel is null) return; // 检查 ImageModel 是否为 null，如果是 直接返回
                    if (e.ChangedButton == MouseButton.Left && IsDrawLines) // 检查左键状态和绘制状态
                    {
                        Point endPoint = e.GetPosition(DrawCanvas);
                        // 正在绘制直线段的逻辑
                        CurrentLin.X2 = point.X;
                        CurrentLin.Y2 = point.Y;
                        // 计算距离并显示
                        MeasureDistance();

                        // 完成一条线段的绘制后，把 CurrentLin 设置为 null，以便下一次创建新线段
                        CurrentLin = null;
                        IsDrawLines = false;
                    }
                }

                else if (CurrentDrawType == ShapeType.Box && IsDrawBox)//如果当前选择的绘制类型是（Box）且正在绘制
                {
                    // 创建Box对象
                    var box = new ShapeBox()
                    {
                        X = Canvas.GetLeft(RectangleShape),
                        Y = Canvas.GetTop(RectangleShape),
                        Width = RectangleShape.Width,
                        Height = RectangleShape.Height,
                        TypeName = MainWindow.Instance.MainModel.ToolConfig.GetCurrentTypeName().TypeName,
                    };

                    //过滤面积过小的box，将面积大于 15的添加到 ImageModel.Shapes 
                    if (box.Width * box.Height > 15)
                    {
                        ImageModel.Shapes.Add(box);
                    }
                    // 删除选择框
                    DrawCanvas.Children.Remove(RectangleShape);
                    IsDrawBox = false;
                    RectangleShape.Width = 1;
                    RectangleShape.Height = 1;
                }
            }
            else if (CurrentDrawType == ShapeType.ColorPicker && isDrawingColorBox)
            {
                // 计算颜色框的区域
                var boxRect = new Rect(Canvas.GetLeft(RectangleShape), Canvas.GetTop(RectangleShape), RectangleShape.Width, RectangleShape.Height);
                var avgColor = GetAverageColorInRectangle(boxRect);
                var point = e.GetPosition(DrawCanvas); //获取鼠标相对 DrawCanvas 的位置。
                // // 获取当前图像的 WriteableBitmap
                //WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

                //// 获取鼠标点击位置的颜色
                //var color = GetColorAtPixel((int)point.X, (int)point.Y, writeableBitmap);

                //// 在这里可以处理获取的颜色，例如显示颜色信息
                //ShowColorResultWindow(color);
                // 显示颜色结果
                ShowColorResultWindow(avgColor);

                // 清理绘制的矩形框
                DrawCanvas.Children.Remove(RectangleShape);
                isDrawingColorBox = false;
            }

            else if (e.ChangedButton == MouseButton.Right)//如果鼠标按钮是右键
            {
                //绘制多边形任务下
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
                Canvas.SetLeft(txtArea, 10); // 设置位置
                Canvas.SetTop(txtArea, 10);
                DrawCanvas.Children.Add(txtArea); // 添加到 Canvas 中


            }

        }

        /// <summary>
        /// 鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (this.ImageModel is null) return;
            if (!IsDrawBox && !IsDrawPolygon) return;
            var point = e.GetPosition(DrawCanvas);
            if (IsDrawPolygon)
            {
                // 绘制多边形框的逻辑
                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;
            }
            else if (CurrentDrawType == ShapeType.Angle)
            {
                // 如果正在绘制第一条线，更新终点
                if (isDrawingFirstLine)
                {
                    firstLine.X2 = point.X;
                    firstLine.Y2 = point.Y;

                    // 更新延长线
                    if (tempLine1 != null)
                    {
                        tempLine1.X2 = point.X + (point.X - firstLine.X1);
                        tempLine1.Y2 = point.Y + (point.Y - firstLine.Y1);
                    }
                }
                // 如果正在绘制第二条线，更新终点
                else if (isDrawingSecondLine)
                {
                    secondLine.X2 = point.X;
                    secondLine.Y2 = point.Y;
                }

            }
            else if (CurrentDrawType == ShapeType.Box && IsDrawBox )
            {
                // 绘制矩形框的逻辑
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
                // 更新矩形框的大小
                double width = point.X - initialPoint.X;
                double height = point.Y - initialPoint.Y;

                RectangleShape.Width = Math.Abs(width);
                RectangleShape.Height = Math.Abs(height);

                // 更新框的位置
                Canvas.SetLeft(RectangleShape, Math.Min(point.X, initialPoint.X));
                Canvas.SetTop(RectangleShape, Math.Min(point.Y, initialPoint.Y));
            }
        
            else if (CurrentDrawType == ShapeType.Lines)
            {
                Point movePoint = e.GetPosition(DrawCanvas);  // 更改为 movePoint
                // 绘制直线段的逻辑
                CurrentLin.X2 = point.X;
                CurrentLin.Y2 = point.Y;

            }

        }
        // 测量结果文本对象
        TextBlock txtMeasure;
        /// <summary>
        /// 创建测量结果文本
        /// </summary>
        /// <param name="point"></param>
        /// / 在绘制直线后调用此方法进行测量距离并显示
        /// 
        public void CreateMeasure(Point point)
        {
            //if (!isMeasure)
            //{
            //    return;
            //}
            // 新建文本控件并添加
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
        // 更新测量结果文本位置和内容
        public void MeasureDistance()
        {
            if (CurrentDrawType == ShapeType.Lines && measureList.Count > 0)
            {
                Point p1 = new Point(CurrentLin.X1, CurrentLin.Y1);
                Point p2 = new Point(CurrentLin.X2, CurrentLin.Y2);
                double distance = GetDistance(p1, p2, 1.0, 1.0); // 假设缩放比例为1
                TextBlock txtMeasure = measureList[measureList.Count - 1];
                txtMeasure.Text = $"{distance.ToString("0.000")}mm";
                // 计算直线段中点位置
                double centerX = (CurrentLin.X1 + CurrentLin.X2) / 2;
                double centerY = (CurrentLin.Y1 + CurrentLin.Y2) / 2;

                // 更新文本位置为直线段中点
                Canvas.SetLeft(txtMeasure, centerX);
                Canvas.SetTop(txtMeasure, centerY);
            }
        }

        /// <summary>
        /// 求平面中两点之间距离
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <param name="dpiX">X 轴缩放比例</param>
        /// <param name="dpiY">Y 轴缩放比例</param>
        /// <returns></returns>
        /// 
        public static double GetDistance(Point p1, Point p2, double dpiX, double dpiY)
        {
            double result = 0;
            result = Math.Sqrt((p1.X * dpiX - p2.X * dpiX) * (p1.X * dpiX - p2.X * dpiX) +
                               (p1.Y * dpiY - p2.Y * dpiY) * (p1.Y * dpiY - p2.Y * dpiY));
            return result;
        }



        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void DrawCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.ImageModel is null) return;
            if (IsDrawPolygon)
            {  // 清除多边形的逻辑
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
      
    /// <summary>
    /// 将绘制的多边形（由线条构成）转换为一个 ShapePolygon 对象
    /// </summary>
    /// <returns></returns>
    private ShapePolygon ConvertToShapePolygon()
        {
            var x = double.MaxValue;
            var y = double.MaxValue;
            foreach (var item in PolygonShape)
            {
                if(item.X1 < x)
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
        /// <summary>
        /// 清除当前的辅助线
        /// </summary>
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
                return Colors.Transparent;  // 如果点击位置在图像外部，返回透明色
            }

            // 获取像素数据
            int stride = writeableBitmap.PixelWidth * 4;  // 假设是 BGRA 格式
            byte[] pixels = new byte[stride * writeableBitmap.PixelHeight];
            writeableBitmap.CopyPixels(pixels, stride, 0);

            int pixelIndex = (y * stride) + (x * 4);  // 计算像素的偏移位置

            byte b = pixels[pixelIndex];     // Blue
            byte g = pixels[pixelIndex + 1]; // Green
            byte r = pixels[pixelIndex + 2]; // Red
            byte a = pixels[pixelIndex + 3]; // Alpha

            return Color.FromArgb(a, r, g, b);
        }


        private Color GetAverageColorInRectangle(Rect rect)
        {
            // 获取当前图像的 WriteableBitmap
            WriteableBitmap writeableBitmap = GetWriteableBitmapFromImageSource(DrawImageBox);

            int startX = (int)Math.Floor(rect.X);
            int startY = (int)Math.Floor(rect.Y);
            int width = (int)rect.Width;
            int height = (int)rect.Height;

            // 确保矩形区域在图像范围内
            if (startX < 0) startX = 0;
            if (startY < 0) startY = 0;
            if (startX + width > writeableBitmap.PixelWidth) width = writeableBitmap.PixelWidth - startX;
            if (startY + height > writeableBitmap.PixelHeight) height = writeableBitmap.PixelHeight - startY;

            long sumR = 0, sumG = 0, sumB = 0;
            long pixelCount = 0;

            // 遍历矩形区域的每个像素
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    var color = GetColorAtPixel(x, y, writeableBitmap);  // 使用之前的 GetColorAtPixel 方法获取每个像素的颜色
                    sumR += color.R;
                    sumG += color.G;
                    sumB += color.B;
                    pixelCount++;
                }
            }

            // 计算平均颜色
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
                Width = 250,  // 固定宽度
                Height = 150, // 固定高度
                ResizeMode = ResizeMode.NoResize, // 禁止调整窗口大小
                WindowStartupLocation = WindowStartupLocation.Manual // 手动设置窗口位置
            };

            // 创建垂直布局的面板
            var stackPanel = new StackPanel
            {
                
                Margin = new Thickness(10)
            };

            // 创建 RGB 和 Hex 值文本块
            var textBlock = new TextBlock
            {
                Text = $"RGB: {avgColor.R}, {avgColor.G}, {avgColor.B}\nHex: {ColorToHex(avgColor)}",
                FontSize = 14,
                Foreground = Brushes.Black
            };

            // 创建颜色块显示当前颜色
            var colorBlock = new Rectangle
            {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(avgColor),
                Margin = new Thickness(0, 5, 0, 0) // 色块与文本之间的间距
            };

            // 将文本和颜色块添加到面板中
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(colorBlock);

            // 将面板设置为窗口的内容
            resultWindow.Content = stackPanel;

            // 获取鼠标位置并设置窗口位置
            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow); // 相对于主窗口的坐标
            var screenPosition = Application.Current.MainWindow.PointToScreen(mousePosition); // 转换为屏幕坐标
            resultWindow.Left = screenPosition.X + 10; // 窗口位置略微偏移鼠标位置（避免直接覆盖鼠标）
            resultWindow.Top = screenPosition.Y + 10;

            // 显示窗口
            resultWindow.ShowDialog();
        }

        // 将 Color 转换为 Hex 格式
        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }



    }

}


#endregion
// <summary>
/// 创造线段图形信息
/// </summary>
//public class LineInfo : CanvasOption
//{
//    private Line line; // 声明 line 变量在类的作用域内

//    /// <summary>
//    /// 创建线段
//    /// </summary>
//    /// <param name="point">起点坐标</param>
//    public void CreateLine(Point point)
//    {
//        line = new Line
//        {
//            //线段颜色
//            Stroke = Brushes.Blue,
//            //线段粗细
//            StrokeThickness = 2,
//            //圆角顶点
//            StrokeLineJoin = PenLineJoin.Round,
//            StrokeEndLineCap = PenLineCap.Round,
//            StrokeStartLineCap = PenLineCap.Round,
//            //起点的X、Y坐标
//            X1 = point.X,
//            Y1 = point.Y,
//            //终点的X、Y坐标
//            X2 = point.X,
//            Y2 = point.Y,
//        };

//        Canvas canvas = new Canvas();
//        canvas.Children.Add(line);
//        DrawCanvas.Children.Add(canvas);
//    }

//    /// <summary>
//    /// 绘制图形
//    /// </summary>
//    /// <param name="point">当前坐标</param>
//    //public override void DrawShape(Point point)
//    //{
//    //    if (line != null)
//    //    {
//    //        line.X2 = point.X;
//    //        line.Y2 = point.Y;
//    //    }
//    //}

//    /// <summary>
//    /// 绘制控制点
//    /// </summary>
//    /// <param name="point">鼠标坐标</param>
//    /// <param name="tag">控制点标志</param>
//    /// <returns></returns>
//    private Dictionary<string, Ellipse> CtrlPoints = new Dictionary<string, Ellipse>();

//    public Ellipse DrawCtrlPoint(Point point, string tag)
//    {
//        Ellipse ctrlPoint = new Ellipse
//        {
//            StrokeThickness = 2, // 设置边框宽度
//            Stroke = Brushes.Black, // 设置边框颜色
//            Fill = Brushes.White, // 设置填充颜色
//            Width = 10, // 设置宽度
//            Height = 10, // 设置高度
//            Margin = new Thickness(point.X - 5, point.Y - 5, point.X - 5, point.Y - 5),
//            Visibility = Visibility.Hidden
//        };

//        if (!CtrlPoints.ContainsKey(tag))
//        {
//            CtrlPoints.Add(tag, ctrlPoint);
//            DrawCanvas.Children.Add(ctrlPoint);
//            ctrlPoint.Tag = tag;
//        }

//        return ctrlPoint;
//    }
//}


