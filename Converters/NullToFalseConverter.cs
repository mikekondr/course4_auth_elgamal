using System.Globalization;
using System.Windows.Data;

namespace auth_elgamal.Converters
{
    public class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Якщо value не null, повертаємо true (панель активна)
            // Якщо value є null, повертаємо false (панель вимкнена)
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}