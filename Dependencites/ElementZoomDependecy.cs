using wpf522.Expends;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace wpf522.Dependencites
{
    public class ElementZoomDependecy : DependencyObject
    {

        public static readonly DependencyProperty IsAutoZoomProperty = DependencyProperty.RegisterAttached("IsAutoZoom", typeof(bool), typeof(ElementZoomDependecy), new PropertyMetadata(IsZoomValueChangedCallBackHandle));



        public static readonly DependencyProperty ScaleTrickProperty = DependencyProperty.RegisterAttached("ScaleTrick", typeof(double), typeof(ElementZoomDependecy), new PropertyMetadata(0.2));



        public static readonly DependencyProperty MinScaleValueProperty = DependencyProperty.RegisterAttached("MinScaleValue", typeof(double), typeof(ElementZoomDependecy), new PropertyMetadata(0.2));



        public static readonly DependencyProperty MaxScaleValueProperty = DependencyProperty.RegisterAttached("MaxScaleValue", typeof(double), typeof(ElementZoomDependecy), new PropertyMetadata(5.0));



        public static readonly DependencyProperty IsCtrlKeyProperty = DependencyProperty.RegisterAttached("IsCtrlKey", typeof(bool), typeof(ElementZoomDependecy), new PropertyMetadata(true));



        public static Dictionary<FrameworkElement, bool> ElementCtrlKey = new Dictionary<FrameworkElement, bool>();






        private static void IsZoomValueChangedCallBackHandle(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if ((bool)e.NewValue == true)
            {
                element.Focusable = true;
                element.MouseUp += Element_MouseUp;
                element.MouseWheel += Element_MouseWheel;
                element.KeyDown += Element_KeyDown;
                element.KeyUp += Element_KeyUp;
                ElementCtrlKey.Add(element, false);
            }
            else
            {
                element.MouseUp -= Element_MouseUp;
                element.MouseWheel -= Element_MouseWheel;
                element.KeyDown -= Element_KeyDown;
                element.KeyUp -= Element_KeyUp;
                ElementCtrlKey.Remove(element);
            }

        }





        private static void Element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                (sender as FrameworkElement).Focus();
            }
        }

        private static void Element_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (e.SystemKey == System.Windows.Input.Key.LeftCtrl || e.SystemKey == System.Windows.Input.Key.RightCtrl)
            {
                if (ElementCtrlKey.ContainsKey(element))
                {
                    ElementCtrlKey[element] = false;
                }
            }
        }





        private static void Element_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var element = sender as FrameworkElement;
            if(e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl)
            {
                if (e.KeyStates == System.Windows.Input.KeyStates.Down && ElementCtrlKey.ContainsKey(element))
                {
                    ElementCtrlKey[element] = true;
                }
                Debug.WriteLine(e.Key + "--" + e.SystemKey + "--");
            }
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                var group = element.RenderTransform as TransformGroup;
                if (group is not null)
                {
                    var scale = group.Children.FindTargetType<ScaleTransform, Transform>();
                    if(scale is not null)
                    {
                        scale.ScaleX = 1;
                        scale.ScaleY = 1;
                    }
                }
            }
        }





        private static void Element_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element is null || GetIsAutoZoom(element) == false)
            {
                return;
            }
            if(GetIsCtrlKey(element) && ElementCtrlKey.ContainsKey(element) && ElementCtrlKey[element] != true)
            {
                return;
            }
            Console.WriteLine("¹öÂÖµÄÖµ : {0}", e.Delta);

            TransformGroup group = null;
            ScaleTransform scale = null;
            if (element.RenderTransform is null || element.RenderTransform is not TransformGroup)
            {
                group = new TransformGroup();
                scale = new ScaleTransform();
                scale.ScaleX = 1;
                scale.ScaleY = 1;   
                element.RenderTransform = group;
                group.Children.Add(scale);
            }
            else
            {
                group = element.RenderTransform as TransformGroup;
                scale = group.Children.FindTargetType<ScaleTransform, Transform>();
            }
            if(group is null || scale is null)
            {
                return;
            }

            var value = e.Delta / 120.0;
            var trick = GetScaleTrick(element);
            var point = e.GetPosition(element);
            scale.CenterX = point.X;
            scale.CenterY = point.Y;
            if (value > 0)
            {
                scale.ScaleX += trick;
                scale.ScaleY += trick;
                if (scale.ScaleX > GetMaxScaleValue(element))
                {
                    scale.ScaleX = GetMaxScaleValue(element);
                }
                if (scale.ScaleY > GetMaxScaleValue(element))
                {
                    scale.ScaleY = GetMaxScaleValue(element);
                }
            }
            else
            {
                if(scale.ScaleX > trick && scale.ScaleY > trick)
                scale.ScaleX -= trick;
                scale.ScaleY -= trick;
                if (scale.ScaleX < GetMinScaleValue(element))
                {
                    scale.ScaleX = GetMinScaleValue(element);
                }
                if (scale.ScaleY < GetMinScaleValue(element))
                {
                    scale.ScaleY = GetMinScaleValue(element);
                }
            }
            
        }

        public static bool GetIsAutoZoom(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(IsAutoZoomProperty);
        }


        public static void SetIsAutoZoom(DependencyObject dependency, bool value)
        {
            dependency.SetValue(IsAutoZoomProperty, value);
        }

        public static bool GetIsCtrlKey(DependencyObject dependency)
        {
            return (bool)dependency.GetValue(IsCtrlKeyProperty);
        }

        public static void SetIsCtrlKey(DependencyObject dependency, bool value)
        {
            dependency.SetValue(IsCtrlKeyProperty, value);
        }

        public static double GetScaleTrick(DependencyObject dependency)
        {
            return (double)dependency.GetValue(ScaleTrickProperty);
        }

        public static void SetScaleTrick(DependencyObject dependency, double value)
        {
            dependency.SetValue(ScaleTrickProperty, value);
        }

        public static double GetMinScaleValue(DependencyObject dependency)
        {
            return (double)dependency.GetValue(MinScaleValueProperty);
        }

        public static void SetMinScaleValue(DependencyObject dependency, double value)
        {
            dependency.SetValue(MinScaleValueProperty, value);
        }

        public static double GetMaxScaleValue(DependencyObject dependency)
        {
            return (double)dependency.GetValue(MaxScaleValueProperty);
        }

        public static void SetMaxScaleValue(DependencyObject dependency, double value)
        {
            dependency.SetValue(MaxScaleValueProperty, value);
        }
    }
}

