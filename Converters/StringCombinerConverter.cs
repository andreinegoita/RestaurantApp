using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class StringCombinerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if we have at least two values (quantity and unit)
            if (values.Length >= 2 && values[0] != null && values[1] != null)
            {
                return $"{values[0]} {values[1]}";
            }

            // If only one value is available (e.g., quantity)
            if (values.Length >= 1 && values[0] != null)
            {
                return values[0].ToString();
            }

            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
