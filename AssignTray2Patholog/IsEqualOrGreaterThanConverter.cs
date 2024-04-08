using System;
using System.Globalization;
using System.Windows.Data;

namespace AssignTray2Patholog
{
    public class IsEqualOrGreaterThanConverter : IValueConverter
    {
        public static readonly IValueConverter Instance = new IsEqualOrGreaterThanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intValue = (int)value;
            int compareToValue = 0;

            return intValue >= compareToValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
           throw new NotImplementedException();
        }
    }
}