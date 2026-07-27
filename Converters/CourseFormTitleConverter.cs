using System.Globalization;
using System.Windows.Data;

namespace StudentManagementSystem.Converters;

public sealed class CourseFormTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Edit Course" : "Add Course";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
