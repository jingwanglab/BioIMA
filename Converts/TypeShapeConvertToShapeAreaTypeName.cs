using wpf522.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpf522.Converts
{
    public class TypeShapeConvertToShapeAreaTypeName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string shapeTypeName = value as string;
            if (shapeTypeName == null || MainWindow.Instance is null) return null;
            foreach (var item in MainWindow.Instance.MainModel.ToolConfig.ShapeTypeColorStructs)
            {
                if (item.TypeName.Equals(shapeTypeName))
                {
                    return item;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as ShapeTypeColorStruct).TypeName;
        }
    }
}
