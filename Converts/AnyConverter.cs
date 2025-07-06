using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpf522.Converts
{
    public class AnyConverter : IValueConverter
    {



        public List<AnyItem> AnyConverts { get; set; } = new List<AnyItem>();



        public AnyOtherItem AnyOther { get; set; } = null;


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var item in AnyConverts)
            {
                if (value is null || item.From is null)
                {
                    if (object.ReferenceEquals(item.From, value))
                    {
                        return item.To;
                    }
                }
                else if (value is not null && value.Equals(item.From))
                {
                    return item.To;
                }
                else if (item.From is not null && item.From.Equals(value))
                {
                    return item.To;
                }
            }
            if(AnyOther is not null)
            {
                return AnyOther.To;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var item in AnyConverts)
            {
                if (object.ReferenceEquals(item.To, value))
                {
                    return item.From;
                }
            }
            return null;
        }
    }
}

