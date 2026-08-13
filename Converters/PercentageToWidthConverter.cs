using System.Globalization;
using System.Windows.Data;

namespace StudentManagementSystem.Converters;

public sealed class PercentageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percentage = value is int i ? i : 0;
        var maxWidth = parameter is string s && double.TryParse(s, out var w) ? w : 300;
        return percentage * maxWidth / 100.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
