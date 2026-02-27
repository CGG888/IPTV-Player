using System;
using System.Globalization;
using System.Windows.Data;

namespace LibmpvIptvClient
{
    public class RangeToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4) return 0d;
            if (!(values[0] is double total)) return 0d;
            if (!(values[1] is double value)) return 0d;
            if (!(values[2] is double min)) return 0d;
            if (!(values[3] is double max)) return 0d;
            var range = max - min;
            if (range <= 0.000001) return 0d;
            var ratio = (value - min) / range;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio)) ratio = 0d;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            return total * ratio;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}
