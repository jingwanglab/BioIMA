using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace wpf522.Expends
{
    public static class BindingExpend
    {

        /// <summary>
        /// 设置绑定
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourcePath"></param>
        /// <param name="target"></param>
        /// <param name="targetDenpendency"></param>
        /// <param name="mode"></param>
        /// <param name="converter"></param>
        public static void BindingEx(this object source, string sourcePath, DependencyObject target, DependencyProperty targetDenpendency, BindingMode mode = BindingMode.TwoWay, IValueConverter converter = null)
        {
            Binding binding = new Binding() { Source = source, };
            binding.Mode = mode;
            binding.Converter = converter;
            binding.Path = new System.Windows.PropertyPath(sourcePath);

            BindingOperations.SetBinding(target, targetDenpendency, binding);
        }

    }
}
