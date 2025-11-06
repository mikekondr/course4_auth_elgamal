using System.Globalization;
using System.Windows.Data;

namespace auth_elgamal.Converters
{
    /// <summary>
    /// Конвертує значення null у false та будь-яке інше значення у true.
    /// для керування активністю панелі в залежності від наявності даних.
    /// </summary>
    public class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}