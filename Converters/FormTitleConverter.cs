using System.Globalization;
using System.Windows.Data;

namespace StudentManagementSystem.Converters;

public sealed class FormTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Edit Student" : "Register Student";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
