using System;
using System.Globalization;
using System.Windows.Data;

namespace EmailClient.Gui.Converter;

[ValueConversion(typeof(string), typeof(string))]
public class EmptyStringFallbackConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value.ToString();
        return string.IsNullOrEmpty(str) ? parameter.ToString() : str;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
