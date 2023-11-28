using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace EmailClient.Gui.Converter;

[ValueConversion(typeof(IEnumerable<object>), typeof(string))]
public class ListJoinerConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var emailList = (IEnumerable<object>)value;
        return string.Join(", ", emailList.Select(e => e.ToString()));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}