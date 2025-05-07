using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            switch (status?.ToLower())
            {
                case "pending":
                    return new SolidColorBrush(Colors.Orange);
                case "preparing":
                    return new SolidColorBrush(Colors.Blue);
                case "out for delivery":
                    return new SolidColorBrush(Colors.Purple);
                case "delivered":
                    return new SolidColorBrush(Colors.Green);
                case "cancelled":
                    return new SolidColorBrush(Colors.Red);
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
