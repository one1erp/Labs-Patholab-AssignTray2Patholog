using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AssignTray2Patholog
{
    public class IconStatusConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {

                string iconPath = Patholab_Common.Utils.GetResourcePath();
                iconPath += parameter + value.ToString() + ".ico";
                return iconPath;

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
