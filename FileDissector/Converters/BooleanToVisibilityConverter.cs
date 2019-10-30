using System;
using System.Globalization;
using System.Windows;

namespace FileDissector.Converters
{
    public class BooleanToVisibilityConverter : ConverterMarkupExtension<BooleanToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility vis && vis == Visibility.Visible;
        }
    }
}
