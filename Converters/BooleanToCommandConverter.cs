using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Input;

namespace RestaurantApp.Converters
{
    public class BooleanToCommandMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return null;

            if (values[0] is bool isEditing && values[1] is object dataContext && parameter is string paramString)
            {
                var parts = paramString.Split('|');
                if (parts.Length == 2)
                {
                    var commandName = isEditing ? parts[0] : parts[1];

                    var commandProperty = dataContext.GetType().GetProperty(commandName);
                    return commandProperty?.GetValue(dataContext) as ICommand;
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
