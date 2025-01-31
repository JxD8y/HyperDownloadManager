using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HyperDownloadManager.Converters
{
    public class TimeSpantoString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan)
            {
                TimeSpan _ts = (TimeSpan)value;
                return $"{_ts.Days}:{_ts.Hours.ToString("00")}:{_ts.Minutes.ToString("00")}:{_ts.Seconds.ToString("00")}";
            }
            return "000000:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
