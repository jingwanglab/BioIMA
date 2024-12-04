using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace wpf522.Expends
{
    public static class ElementExpends
    {

        /// <summary>
        /// 查找元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T SearchElement<T>(this DependencyObject element) where T : DependencyObject
        {
            var pa = VisualTreeHelper.GetParent(element) as DependencyObject;
            while (pa != null && pa is not T)
            {
                pa = VisualTreeHelper.GetParent(pa) as DependencyObject;
            }
            return pa as T;
        }


    }
}
