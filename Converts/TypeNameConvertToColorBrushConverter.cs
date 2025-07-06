using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace wpf522.Converts
{
    internal class TypeNameConvertToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var item in MainWindow.Instance.MainModel.ToolConfig.ShapeTypeColorStructs)
            {
                if(value is not null && value.Equals(item.TypeName))
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.Color));
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

