using System;
using System.Globalization;
using System.Windows.Data;

namespace FileDissector.Converters
{
    public class EqualityToBooleanConverter : ConverterMarkupExtension<EqualityToBooleanConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(parameter);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
