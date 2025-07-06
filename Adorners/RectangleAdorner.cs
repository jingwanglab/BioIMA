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
    public class RectangleAdorner : Adorner
    {

        Thumb _leftThumb, _topThumb, _rightThumb, _bottomThumb;

        Thumb _lefTopThumb, _rightTopThumb, _rightBottomThumb, _leftbottomThumb;

        Grid _grid;
        UIElement _adornedElement;
        public RectangleAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _adornedElement = adornedElement;

            _leftThumb = new Thumb();
            _leftThumb.HorizontalAlignment = HorizontalAlignment.Left;
            _leftThumb.VerticalAlignment = VerticalAlignment.Center;
            _leftThumb.Cursor = Cursors.SizeWE;
            _topThumb = new Thumb();
            _topThumb.HorizontalAlignment = HorizontalAlignment.Center;
            _topThumb.VerticalAlignment = VerticalAlignment.Top;
            _topThumb.Cursor = Cursors.SizeNS;
            _rightThumb = new Thumb();
            _rightThumb.HorizontalAlignment = HorizontalAlignment.Right;
            _rightThumb.VerticalAlignment = VerticalAlignment.Center;
            _rightThumb.Cursor = Cursors.SizeWE;
            _bottomThumb = new Thumb();
            _bottomThumb.HorizontalAlignment = HorizontalAlignment.Center;
            _bottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            _bottomThumb.Cursor = Cursors.SizeNS;
            _lefTopThumb = new Thumb();
            _lefTopThumb.HorizontalAlignment = HorizontalAlignment.Left;
            _lefTopThumb.VerticalAlignment = VerticalAlignment.Top;
            _lefTopThumb.Cursor = Cursors.SizeNWSE;
            _rightTopThumb = new Thumb();
            _rightTopThumb.HorizontalAlignment = HorizontalAlignment.Right;
            _rightTopThumb.VerticalAlignment = VerticalAlignment.Top;
            _rightTopThumb.Cursor = Cursors.SizeNESW;
            _rightBottomThumb = new Thumb();
            _rightBottomThumb.HorizontalAlignment = HorizontalAlignment.Right;
            _rightBottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            _rightBottomThumb.Cursor = Cursors.SizeNWSE;
            _leftbottomThumb = new Thumb();
            _leftbottomThumb.HorizontalAlignment = HorizontalAlignment.Left;
            _leftbottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            _leftbottomThumb.Cursor = Cursors.SizeNESW;
            _grid = new Grid();
            _grid.Children.Add(_leftThumb);
            _grid.Children.Add(_topThumb);
            _grid.Children.Add(_rightThumb);
            _grid.Children.Add(_bottomThumb);
            _grid.Children.Add(_lefTopThumb);
            _grid.Children.Add(_rightTopThumb);
            _grid.Children.Add(_rightBottomThumb);
            _grid.Children.Add(_leftbottomThumb);
            AddVisualChild(_grid);
            foreach (Thumb thumb in _grid.Children)
            {
                thumb.Width = 8;
                thumb.Height = 8;
                thumb.Background = Brushes.Green;
                thumb.Template = new ControlTemplate(typeof(Thumb))
                {
                    VisualTree = GetFactory(new SolidColorBrush(Colors.White))
                };
                thumb.DragDelta += Thumb_DragDelta;
            }
        }
        protected override Visual GetVisualChild(int index)
        {
            return _grid;
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }
        protected override Size ArrangeOverride(Size finalSize)
        {

            var rect = new Rect(new Point(-_leftThumb.Width / 2, -_leftThumb.Height / 2), new Size(finalSize.Width + _leftThumb.Width, finalSize.Height + _leftThumb.Height));
            _grid.Arrange(rect);
            return finalSize;
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var c = _adornedElement as FrameworkElement;
            var thumb = sender as FrameworkElement;
            double left, top, width, height;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
            {

                left = double.IsNaN(Canvas.GetLeft(c)) ? 0 : Canvas.GetLeft(c) + e.HorizontalChange;
                width = c.Width - e.HorizontalChange;
            }
            else
            {
                left = Canvas.GetLeft(c);
                width = c.Width + e.HorizontalChange;
            }
            if (thumb.VerticalAlignment == VerticalAlignment.Top)
            {
                top = double.IsNaN(Canvas.GetTop(c)) ? 0 : Canvas.GetTop(c) + e.VerticalChange;
                height = c.Height - e.VerticalChange;
            }
            else
            {
                top = Canvas.GetTop(c);
                height = c.Height + e.VerticalChange;
            }
            if (thumb.HorizontalAlignment != HorizontalAlignment.Center)
            {
                if (width >= 0)
                {
                    Canvas.SetLeft(c, left);
                    c.Width = width;
                }
            }
            if (thumb.VerticalAlignment != VerticalAlignment.Center)
            {
                if (height >= 0)
                {
                    Canvas.SetTop(c, top);
                    c.Height = height;
                }
            }
        }

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

