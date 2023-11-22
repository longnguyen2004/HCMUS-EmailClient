using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace EmailClient.Gui.Converter;

[ValueConversion(typeof(IEnumerable<Email.EmailAddress>), typeof(string))]
public class EmailListConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var emailList = (IEnumerable<Email.EmailAddress>)value;
        return string.Join(", ", emailList.Select(email => email.ToString()));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}