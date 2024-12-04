using wpf522.Adorners;
using wpf522.Converts;
using wpf522.Dependencites.DataStructs;
using wpf522.Expends;
using wpf522.Models;
using wpf522.Models.DrawShapes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace wpf522.Dependencites
{
    public class ShapeAutoCreateCanvasDependency : DependencyObject
    {

        public class CanvasSelectedInfo
        {
            /// <summary>
            /// 矩形框
            /// </summary>
            public Rectangle Rectangle { get; set; } = new Rectangle();
            /// <summary>
            /// 是否开始绘制
            /// </summary>
            public bool IsStartSelectedDraw { get; set; } = false;
            /// <summary>
            /// 所有被选中的形状
            /// </summary>
            public List<Shape> SelectedShapes { get; set; } = new List<Shape>();
            /// <summary>
            /// 所有被选中的形状区域
            /// </summary>
            public List<ShapeArea> SelectedShapeAreas { get; set; } = new List<ShapeArea>();
            /// <summary>
            /// 选择的区域
            /// </summary>
            public Rect SelectedRect { get; set; } = new Rect();
        }
        /// <summary>
        /// 列表数据源变更事件
        /// </summary>
        public static readonly DependencyProperty ShapeCollectionSourceProperty = DependencyProperty.RegisterAttached("ShapeCollectionSource", typeof(ObservableCollection<ShapeArea>), typeof(ShapeAutoCreateCanvasDependency), new PropertyMetadata(new PropertyChangedCallback(ShapeCollectionPropertyChanged)));
        /// <summary>
        /// 多边形点移动事件
        /// </summary>
        public static readonly DependencyProperty PolygonShapePointMoveEventProperty = DependencyProperty.RegisterAttached("PolygonShapePointMoveEvent", 
            typeof(Action<PolygonPointChange>), typeof(ShapeAutoCreateCanvasDependency));
        /// <summary>
        /// 笔触
        /// </summary>
        public static readonly DependencyProperty ShapeStrokeProperty = DependencyProperty.RegisterAttached("ShapeStroke", typeof(System.Windows.Media.Brush), typeof(ShapeAutoCreateCanvasDependency));
        /// <summary>
        /// 笔触 粗细
        /// </summary>
        public static readonly DependencyProperty ShapeStrokeThicknessProperty = DependencyProperty.RegisterAttached("ShapeStrokeThickness", typeof(int), typeof(ShapeAutoCreateCanvasDependency));
        /// <summary>
        /// 是否进入选择模式
        /// </summary>
        public static readonly DependencyProperty IsSelectedModeProperty = DependencyProperty.RegisterAttached("IsSelectedMode", typeof(bool), typeof(ShapeAutoCreateCanvasDependency), new PropertyMetadata(false)); 
        /// <summary>
        /// 已经注册的集合
        /// </summary>
        private static Dictionary<ObservableCollection<ShapeArea>, Canvas> ShapeCollectionRegisters = new Dictionary<ObservableCollection<ShapeArea>, Canvas>();
        /// <summary>
        /// 选择专用矩形框
        /// </summary>
        private static Dictionary<Canvas, CanvasSelectedInfo> CanvasSelectedRect = new Dictionary<Canvas, CanvasSelectedInfo>();
        /// <summary>
        /// 已经注册的集合 对应的canvas中所包含的 形状
        /// </summary>
        private static Dictionary<ObservableCollection<ShapeArea>, Dictionary<ShapeArea, Shape>> ShapeCollectionShapesRegisters = new Dictionary<ObservableCollection<ShapeArea>, Dictionary<ShapeArea, System.Windows.Shapes.Shape>>();
        /// <summary>
        /// 形状数据源变更
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void ShapeCollectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Canvas canvas = (Canvas)d;
            canvas.Children.Clear();
            canvas.Focusable = true;
            var old_collection = e.OldValue as ObservableCollection<ShapeArea>;
            if (old_collection is not null)
            {
                old_collection.CollectionChanged -= Collection_CollectionChanged;
                canvas.MouseDown -= Canvas_MouseDown;
                canvas.MouseMove -= Canvas_MouseMove;
                canvas.MouseLeave -= Canvas_MouseLeave;
                canvas.MouseUp -= Canvas_MouseUp;
                CanvasSelectedRect.Remove(canvas);
                ShapeCollectionRegisters.Remove(old_collection);
                ShapeCollectionShapesRegisters.Remove(old_collection);
            }

            var collection = e.NewValue as ObservableCollection<ShapeArea>;
            if (collection is not null)
            {
                collection.CollectionChanged += Collection_CollectionChanged;
                canvas.MouseDown += Canvas_MouseDown;
                canvas.MouseMove += Canvas_MouseMove;
                canvas.MouseLeave += Canvas_MouseLeave;
                canvas.MouseUp += Canvas_MouseUp;
                CanvasSelectedRect.Add(canvas, new CanvasSelectedInfo());
                ShapeCollectionRegisters.Add(collection, canvas);
                ShapeCollectionShapesRegisters.Add(collection, new Dictionary<ShapeArea, Shape>());
                if(collection.Count > 0)
                {
                    AddShapeAreaToCanvas(canvas, collection, ShapeCollectionShapesRegisters[collection]);
                }
            }
        }


        private static void Canvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            var info = CanvasSelectedRect[canvas];
            if (info.IsStartSelectedDraw == false)
            {
                CancleSelectedShapes(canvas);
            }
            else
            {
                SetSelectedShapes(canvas);
                info.IsStartSelectedDraw = false;
            }
            canvas.ReleaseMouseCapture();
            canvas.Focus();
        }
        private static void Canvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var canvas = sender as Canvas;
            var info = CanvasSelectedRect[canvas];
            var point = e.GetPosition(canvas);
            canvas.ReleaseMouseCapture();   
            if (info.IsStartSelectedDraw == false) return;
            info.IsStartSelectedDraw = false;
            if(canvas.Children.Contains(info.Rectangle))
                canvas.Children.Remove(info.Rectangle);
        }
        /// <summary>
        /// 鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var canvas = sender as Canvas;
            var info = CanvasSelectedRect[canvas];
            var point = e.GetPosition(canvas);
            if (info.IsStartSelectedDraw)
            {
                var old = new Point(Canvas.GetLeft(info.Rectangle)
                , Canvas.GetTop(info.Rectangle));
                var minx = Math.Min(old.X, point.X);
                var miny = Math.Min(old.Y, point.Y);

                Canvas.SetLeft(info.Rectangle, minx);
                Canvas.SetTop(info.Rectangle, miny);
                info.Rectangle.Width = Math.Abs(old.X - point.X);
                info.Rectangle.Height = Math.Abs(old.Y - point.Y);
            }
        }
        /// <summary>
        /// 鼠标按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            var info = CanvasSelectedRect[canvas];
            var point = e.GetPosition(canvas);
            //强制捕获鼠标
            canvas.CaptureMouse();
            //如果是选中模式下
            if (GetIsSelectedMode(canvas))
            {
                canvas.Children.Add(info.Rectangle);
                info.Rectangle.Stroke = new SolidColorBrush(Colors.Red);
                info.Rectangle.StrokeDashArray = new DoubleCollection(new double[] {4, 2 });
                info.IsStartSelectedDraw = true;
                Canvas.SetLeft(info.Rectangle, point.X);
                Canvas.SetTop(info.Rectangle, point.Y);
                info.Rectangle.Width = 0;
                info.Rectangle.Height = 0;
            }
        }
        /// <summary>
        /// 设置被选中的形状
        /// </summary>
        /// <param name="canvas"></param>
        private static void SetSelectedShapes(Canvas canvas)
        {
            var info = CanvasSelectedRect[canvas];
            if (info.IsStartSelectedDraw == false) return;
            canvas.Children.Remove(info.Rectangle);
            info.SelectedRect = new Rect(Canvas.GetLeft(info.Rectangle), Canvas.GetTop(info.Rectangle), info.Rectangle.Width, info.Rectangle.Height);
            foreach (var item in canvas.Children)
            {
                if (item is Shape shape)
                {
                    bool isSelected = (shape is Rectangle && info.SelectedRect.IsContains(shape as Rectangle))
                        || (shape is Polygon && info.SelectedRect.IsContains(shape as Polygon))
                        || (shape is Line && info.SelectedRect.IsContains(shape as Line));

                    if (isSelected)
                    {
                        ShapeSelectDependency.SetShapeSelectEffect(shape, true);
                        info.SelectedShapes.Add(shape);
                        info.SelectedShapeAreas.Add(ShapeSelectDependency.GetDependencyObject(shape) as ShapeArea);
                    }
                    else
                    {
                        ShapeSelectDependency.SetShapeSelectEffect(shape, false);
                        if (info.SelectedShapes.Contains(shape))
                        {
                            info.SelectedShapes.Remove(shape);
                        }
                        var area = ShapeSelectDependency.GetDependencyObject(shape) as ShapeArea;
                        if (info.SelectedShapeAreas.Contains(area))
                        {
                            info.SelectedShapeAreas.Remove(area);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 取消选中效果
        /// </summary>
        /// <param name="canvas"></param>
        private static void CancleSelectedShapes(Canvas canvas)
        {
            var info = CanvasSelectedRect[canvas];
            foreach (var item in info.SelectedShapes)
            {
                ShapeSelectDependency.SetShapeSelectEffect(item, false);
            }
            info.SelectedShapes.Clear();
            info.SelectedShapeAreas.Clear();
        }

        /// <summary>
        /// 删除所有被选中的shape
        /// </summary>
        /// <param name="canvas"></param>
        private static void RemoveSelectedShapes(Canvas canvas)
        {
            var info = CanvasSelectedRect[canvas];
            ObservableCollection<ShapeArea> collection = null;
            foreach (var item in ShapeCollectionRegisters)
            {
                if (item.Value == canvas)
                {
                    collection = item.Key;
                }
            }
            if(collection is not null)
            {
                foreach (var item in info.SelectedShapeAreas)
                {
                    collection.Remove(item);
                }
            }
            foreach (var item in canvas.Children)
            {
                if(item is Shape)
                {
                    ShapeSelectDependency.SetShapeSelectEffect(item as Shape, false);
                }
            }
            info.SelectedShapes.Clear();
        }
        /// <summary>
        /// 集合变更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void Collection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender != null && ShapeCollectionRegisters.ContainsKey(sender as ObservableCollection<ShapeArea>))
            {
                var canvas = ShapeCollectionRegisters[sender as ObservableCollection<ShapeArea>];
                var collection = ShapeCollectionShapesRegisters[sender as ObservableCollection<ShapeArea>];
                //如果是新增
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems is not null)
                {
                    AddShapeAreaToCanvas(canvas, e.NewItems, collection);
                }
                //如果是删除
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems is not null)
                {
                    foreach (ShapeArea item in e.OldItems)
                    {
                        var shape = collection[item];
                        canvas.Children.Remove(shape);
                        collection.Remove(item);
                    }
                }
            }
        }
        /// <summary>
        /// 添加areas 到布局中
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="areas"></param>
        /// <param name="collection"></param>
        public static void AddShapeAreaToCanvas(Canvas canvas, IEnumerable areas, Dictionary<ShapeArea, Shape> collection)
        {
            foreach(ShapeArea item in areas)
            {
                Shape shape = null;
                //如果是矩形框
                if (item is ShapeBox)
                {
                    System.Windows.Shapes.Rectangle rectangle = new Rectangle();
                    rectangle.Stroke = GetShapeStroke(canvas);
                    rectangle.StrokeThickness = GetShapeStrokeThickness(canvas);
                    canvas.Children.Add(rectangle);
                    (item as ShapeBox).ShapeBindingRectangle(rectangle);
                    collection.Add(item, rectangle);
                    shape = rectangle;
                    //添加控制装饰器  允许控件可以拖动
                    var layout = AdornerLayer.GetAdornerLayer(rectangle);
                    layout.Add(new RectangleAdorner(rectangle));
                }
                else if (item is ShapePolygon)
                {
                    System.Windows.Shapes.Polygon polygon = new Polygon();
                    polygon.Stroke = GetShapeStroke(canvas);
                    polygon.StrokeThickness = GetShapeStrokeThickness(canvas);
                    //polygon.Fill = GetShapeStroke(canvas);
                    canvas.Children.Add(polygon);
                    polygon.Points.AppendRange((item as ShapePolygon).Points);
                    //polygon.SizeChanged += Polygon_SizeChanged;
                    //记录变更事件
                    SetPolygonShapePointMoveEvent(polygon, Polygon_SizeChanged);
                    shape = polygon;
                    //绑定偏移
                    (item as ShapePolygon).BindingEx("StartX", polygon, Canvas.LeftProperty);
                    (item as ShapePolygon).BindingEx("StartY", polygon, Canvas.TopProperty);
                    collection.Add(item, polygon);
                    //添加控制装饰器  允许控件可以拖动
                    var polygonLayer = AdornerLayer.GetAdornerLayer(polygon);
                    polygonLayer.Add(new PolygonAdorner(polygon));

                }
                //绑定选中效果
                item.BindingEx("IsSelected", shape, ShapeSelectDependency.ShapeSelectEffectProperty);
                //绑定类型和颜色
                item.BindingEx("TypeName", shape, Shape.StrokeProperty, converter: new TypeNameConvertToColorBrushConverter()); 
                shape.Fill = new SolidColorBrush(new Color() { A = 0, });
                ShapeSelectDependency.SetDependencyObject(shape, item);
                //鼠标抬起
                shape.MouseUp += (o, e) => {
                    //设置选中
                    ShapeSelectDependency.SetShapeSelectEffect(o as Shape, true);
                    e.Handled = true;
                    (o as Shape).ReleaseMouseCapture();
                };
                shape.MouseDown += (o, e) => {
                    (o as Shape).CaptureMouse();
                    e.Handled = true;
                };
            }
        }
        /// <summary>
        /// 当多边形变更大小的时候
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void Polygon_SizeChanged(PolygonPointChange e)
        {
            var sender = e.Polygon;
            if (sender is null || sender is not Polygon) return;
            var polygon = ShapeSelectDependency.GetDependencyObject(sender) as ShapePolygon;
            //polygon.Points.Clear();
            //polygon.Points.AppendRange((sender as Polygon).Points);
            polygon.Points[e.Index] = e.NewValue;
        }

        public static Brush GetShapeStroke(DependencyObject dependency)
        {
            return (Brush)dependency.GetValue(ShapeStrokeProperty);
        }

        public static void SetShapeStroke(DependencyObject dependency, Brush brush)
        {
            dependency.SetValue(ShapeStrokeProperty, brush);
        }

        public static int GetShapeStrokeThickness(DependencyObject dependency)
        {
            return (int)dependency.GetValue(ShapeStrokeThicknessProperty);
        }

        public static void SetShapeStrokeThickness(DependencyObject dependency, int thickness)
        {
            dependency.SetValue(ShapeStrokeThicknessProperty, thickness);
        }


        public static ObservableCollection<ShapeArea> GetShapeCollectionSource(DependencyObject dependency)
        {
            return (ObservableCollection<ShapeArea>)dependency.GetValue(ShapeCollectionSourceProperty);
        }

        public static void SetShapeCollectionSource(DependencyObject dependency, ObservableCollection<ShapeArea> shapes)
        {
            dependency.SetValue(ShapeCollectionSourceProperty, shapes);
        }

        public static bool GetIsSelectedMode(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(IsSelectedModeProperty);
        }

        public static void SetIsSelectedMode(DependencyObject dependency, bool value)
        {
            dependency.SetValue(IsSelectedModeProperty, value);
        }

        public static void SetPolygonShapePointMoveEvent(DependencyObject dependency, Action<PolygonPointChange> e)
        {
            dependency.SetValue(PolygonShapePointMoveEventProperty, e);
        }

        public static Action<PolygonPointChange> GetPolygonShapePointMoveEvent(DependencyObject dependency)
        {
            return (Action<PolygonPointChange>)dependency.GetValue(PolygonShapePointMoveEventProperty);
        }
    }
}
