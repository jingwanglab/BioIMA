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



        public static readonly DependencyProperty DependencyObjectProperty = DependencyProperty.RegisterAttached("DependencyObject", typeof(object), typeof(ShapeSelectDependency), new PropertyMetadata(null));









        private static void ShapeSelectEffectChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Shape) return;
            var shape = d as Shape;

            if (e.NewValue is true)
            {





                shape.Fill = shape.Stroke;
            }
            else
            {




                shape.Fill = new SolidColorBrush(new Color() { A = 0, R = 0, G = 0, B = 0 });
            }
        }





















        public static bool GetShapeSelectEffect(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(ShapeSelectEffectProperty);
        }

        public static void SetShapeSelectEffect(DependencyObject dependency, bool value)
        {
            dependency.SetValue(ShapeSelectEffectProperty, value);
        }









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

