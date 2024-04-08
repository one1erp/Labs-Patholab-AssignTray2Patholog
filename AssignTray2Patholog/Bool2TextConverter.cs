using System;
using System.Globalization;
using System.Windows.Data;

namespace AssignTray2Patholog
{
    public class Bool2TextConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool? b = value as bool?;
                if (b==true)
                {
                    return 
                        "כן";
                }
                return
                    "לא";
            

            }
            catch (Exception exception)
            {

                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}