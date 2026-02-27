using System;
using System.Globalization;
using System.Windows.Data;

namespace LibmpvIptvClient
{
    public class BoolToStarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool fav = value is bool b && b;
                return fav ? "★" : "☆";
            }
            catch
            {
                return "☆";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s == "★";
            }
            return false;
        }
    }
}
