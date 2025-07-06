using wpf522.Expends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace wpf522.Dependencites
{
    public class CanvasDragMoveDependency : DependencyObject
    {



        public class DragParams
        {
            public bool IsDrag { get; set; }



            public Canvas Layout { get; set; }



            public Point PressPoint { get; set; }
        }


        public static readonly DependencyProperty CanDragMoveProperty = DependencyProperty.RegisterAttached("CanDragMove", typeof(bool), typeof(CanvasDragMoveDependency), new PropertyMetadata(PropertyChangedCallBackHadle));



        public static readonly DependencyProperty MouseButtonProperty = DependencyProperty.RegisterAttached("MouseButton", typeof(MouseButton), typeof(CanvasDragMoveDependency), new PropertyMetadata(MouseButton.Left));



        public static readonly Dictionary<FrameworkElement, DragParams> Elements = new Dictionary<FrameworkElement, DragParams>(); 






        private static void PropertyChangedCallBackHadle(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            if (element is not null && (bool)e.NewValue == true)
            {
                element.MouseDown += Element_MouseDown;
                element.MouseMove += Element_MouseMove;
                element.MouseLeave += Element_MouseLeave;
                element.MouseUp += Element_MouseUp;
                Elements.Add(element, new DragParams() { IsDrag = true, Layout = element.SearchElement<Canvas>() });
            }
            else
            {
                element.MouseDown -= Element_MouseDown;
                element.MouseMove -= Element_MouseMove;
                element.MouseLeave -= Element_MouseLeave;
                element.MouseUp -= Element_MouseUp;
                if(Elements.ContainsKey(element))
                    Elements.Remove(element);
            }
        }

        private static void Element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            
            if (sender is null || sender is not FrameworkElement || e.ChangedButton != GetMouseButton(sender as DependencyObject)) return;
            var element = sender as FrameworkElement;
            var ps = Elements[sender as FrameworkElement];
            ps.IsDrag = false;
        }

        private static void Element_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is null || sender is not FrameworkElement) return;
            var element = sender as FrameworkElement;
            var ps = Elements[sender as FrameworkElement];
            ps.IsDrag = false;
        }

        private static void Element_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is null || sender is not FrameworkElement) return;
            var element = sender as FrameworkElement;
            var ps = Elements[sender as FrameworkElement];
            if (ps.IsDrag == false)
            {
                return;
            }
            var btn = GetMouseButton(sender as DependencyObject);
            if (btn  == MouseButton.Left && e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            else if (btn == MouseButton.Right && e.RightButton != MouseButtonState.Pressed)
            {
                return;
            }
            else if (btn == MouseButton.Middle && e.MiddleButton != MouseButtonState.Pressed)
            {
                return;
            }
            var point = e.GetPosition(ps.Layout);
            var dis = new Point(point.X - ps.PressPoint.X, point.Y - ps.PressPoint.Y);
            var x = Canvas.GetLeft(element);
            var y = Canvas.GetTop(element);

            Canvas.SetLeft(element, x + dis.X);
            Canvas.SetTop(element, y + dis.Y);
            ps.PressPoint = point;
        }

        private static void Element_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is null || sender is not FrameworkElement || e.ChangedButton != GetMouseButton(sender as DependencyObject)) return;
            var element = sender as FrameworkElement;
            var ps = Elements[sender as FrameworkElement];
            ps.IsDrag = true;
            ps.PressPoint = e.GetPosition(ps.Layout);
        }



        public static bool GetCanDragMove(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(CanDragMoveProperty);
        }

        public static void SetCanDragMove(DependencyObject dependency, bool value)
        {
            dependency.SetValue(CanDragMoveProperty, value);
        }

        public static MouseButton GetMouseButton(DependencyObject dependency)
        {
            return (MouseButton)dependency.GetValue(MouseButtonProperty);
        }

        public static void SetMouseButton(DependencyObject dependency, MouseButton value)
        {
            dependency.SetValue(MouseButtonProperty, value);
        }
    }
}

