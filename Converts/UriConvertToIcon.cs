using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace wpf522.Converts
{
    public class UriConvertToIcon : IValueConverter
    {

        public BitmapImage LocalIcon { get; set; }

        public BitmapImage RemoteIcon { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uri = value as Uri;
            if(string.IsNullOrEmpty(uri?.Host))
            {
                return LocalIcon;
            }
            else
            {
                return RemoteIcon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
