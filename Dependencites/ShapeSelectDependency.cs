using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using wpf522.Models;

namespace wpf522.Dependencites
{
    public class ShapeSelectDependency : DependencyObject
    {

        public static readonly DependencyProperty ShapeSelectEffectProperty = DependencyProperty.RegisterAttached("ShapeSelectEffect", typeof(bool), typeof(ShapeSelectDependency), new PropertyMetadata(ShapeSelectEffectChangedCallback));
        /// <summary>
        /// 附加对象
        /// </summary>
        public static readonly DependencyProperty DependencyObjectProperty = DependencyProperty.RegisterAttached("DependencyObject", typeof(object), typeof(ShapeSelectDependency), new PropertyMetadata(null));
        ///// <summary>
        ///// 选中效果时候的填充颜色
        ///// </summary>
        //public static readonly DependencyProperty FillStokeProperty = DependencyProperty.RegisterAttached("FillStoke", typeof(Brush), typeof(ShapeSelectDependency), new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// 是否启用选中效果回调
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void ShapeSelectEffectChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Shape) return;
            var shape = d as Shape;

            if (e.NewValue is true)
            {
                //shape.Fill = new SolidColorBrush(new Color() { A = 0, R = 0, G = 0, B = 0 });
                //shape.MouseUp += Shape_MouseUp;
                //shape.GotMouseCapture += Shape_GotFocus;
                //shape.LostMouseCapture += Shape_LostFocus;
                //shape.Focusable = true;
                shape.Fill = shape.Stroke;
            }
            else
            {
                //shape.MouseUp -= Shape_MouseUp;
                //shape.GotMouseCapture -= Shape_GotFocus;
                //shape.LostMouseCapture -= Shape_LostFocus;
                //shape.Focusable = false;
                shape.Fill = new SolidColorBrush(new Color() { A = 0, R = 0, G = 0, B = 0 });
            }
        }
        
        //private static void Shape_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    var shape = sender as Shape;
        //    shape.Fill = new SolidColorBrush(new Color() { A = 0, R = 0, G = 0, B = 0 });
        //}

        //private static void Shape_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    var shape = sender as Shape;
        //    shape.Fill = shape.Stroke;
        //}

        ///// <summary>
        ///// 鼠标抬起
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Shape_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    var shape = sender as Shape;
        //    shape.CaptureMouse();
        //}

        public static bool GetShapeSelectEffect(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(ShapeSelectEffectProperty);
        }

        public static void SetShapeSelectEffect(DependencyObject dependency, bool value)
        {
            dependency.SetValue(ShapeSelectEffectProperty, value);
        }

        //public static Brush GetFillStoke(DependencyObject dependency)
        //{
        //    return (Brush)dependency.GetValue(FillStokeProperty);
        //}

        //public static void SetFillStoke(DependencyObject dependency, Brush value)
        //{
        //    dependency.SetValue(FillStokeProperty, value);
        //}

        public static object GetDependencyObject(DependencyObject dependency)
        {
            return dependency.GetValue(DependencyObjectProperty);
        }

        public static void SetDependencyObject(DependencyObject dependency, object value)
        {
            dependency.SetValue(DependencyObjectProperty, value);
        }

    }
}
