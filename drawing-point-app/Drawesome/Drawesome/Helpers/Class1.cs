using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace Drawesome.Helpers
{
    public class IsRunningConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as bool?;
            if (s == null)
            {
                return "stop.png";
            }
            else if (s.Value)
            {
                return "loading.png";
            }
            else
            {
                return "ok-cloud.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
