using System.Globalization;
using System.Windows.Data;

namespace StudentManagementSystem.Converters;

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
