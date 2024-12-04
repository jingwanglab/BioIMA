using wpf522.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpf522.Converts
{
    public class ImageDirToImageCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ImageContentInfoModel model = value as ImageContentInfoModel;
            if (model.IsDirectory)
            {
                if (parameter.Equals("1"))
                {
                    return model.GetChildrenComplieCount();
                }
                else
                {
                    //return model.GetChildrenCount();
                    return 1;
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
