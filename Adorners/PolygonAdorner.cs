using wpf522.Dependencites;
using wpf522.Dependencites.DataStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace wpf522.Adorners
{
    internal class PolygonAdorner : Adorner
    {
        /// <summary>
        /// 控件
        /// </summary>
        public Polygon? Polygon { get; set; } = null;
        /// <summary>
        /// 布局
        /// </summary>
        public Canvas CanvasLayer { get; set; } = new Canvas();
        /// <summary>
        /// 可控制控件半径
        /// </summary>
        public int ThumbRadiuWith { get; set; } = 4;
        /// <summary>
        /// 控制点集合
        /// </summary>
        public List<Thumb> Thumbs { get; set; } = new List<Thumb>();  
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="adornedElement"></param>
        /// <exception cref="ArgumentException"></exception>
        public PolygonAdorner(UIElement adornedElement) : base(adornedElement)
        {
            if(adornedElement is Polygon)
            {
                Polygon = adornedElement as Polygon;
            }
            else
            {
                throw new ArgumentException("类型异常!");
            }

            AddVisualChild(CanvasLayer);
            if (Polygon is not null)
            {
                foreach (var item in Polygon.Points)
                {
                    Thumb tb = new Thumb();
                    tb.Width = ThumbRadiuWith * 2;
                    tb.Height = ThumbRadiuWith * 2;
                    tb.Background = Brushes.Green;
                    tb.Template = new ControlTemplate(typeof(Thumb))
                    {
                        VisualTree = GetFactory(new SolidColorBrush(Colors.White))
                    };
                    tb.Cursor = Cursors.SizeAll;
                    Canvas.SetLeft(tb, item.X);
                    Canvas.SetTop(tb, item.Y);
                    CanvasLayer.Children.Add(tb);
                    Thumbs.Add(tb);
                    tb.DragDelta += Tb_DragDelta;
                }
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return CanvasLayer;
        }
        /// <summary>
        /// 子元素数量
        /// </summary>
        protected override int VisualChildrenCount => 1;

        /// <summary>
        /// 拖拽
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = (Thumb)sender;
            int index = 0;
            foreach (var item in Thumbs)
            {
                if (item == thumb) break;
                index++;
            }
            //移动的量
            double x = e.HorizontalChange;
            double y = e.VerticalChange;

            Canvas.SetLeft(thumb, Canvas.GetLeft(thumb) + x);
            Canvas.SetTop(thumb, Canvas.GetTop(thumb) + y);

            var oldValue = Polygon.Points[index];
            var newValue = new Point(oldValue.X + x, oldValue.Y + y);
            Polygon.Points[index] = newValue;
            var action = ShapeAutoCreateCanvasDependency.GetPolygonShapePointMoveEvent(Polygon);
            action.Invoke(new PolygonPointChange() { Polygon = Polygon, Index = index, OldValue = oldValue, NewValue = newValue });
        }
        /// <summary>
        /// 裁剪尺寸
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var rect = new Rect(new Point(- ThumbRadiuWith, -ThumbRadiuWith), new Size(finalSize.Width + ThumbRadiuWith, finalSize.Height + ThumbRadiuWith));
            CanvasLayer.Arrange(rect);
            return finalSize;
        }

        //thumb的样式
        FrameworkElementFactory GetFactory(Brush back)
        {
            var fef = new FrameworkElementFactory(typeof(Ellipse));
            fef.SetValue(Ellipse.FillProperty, back);
            fef.SetValue(Ellipse.StrokeProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")));
            fef.SetValue(Ellipse.StrokeThicknessProperty, (double)2);
            return fef;
        }
    }
}
