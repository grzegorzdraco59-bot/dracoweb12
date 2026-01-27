using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ERP.UI.WPF.Converters;

/// <summary>
/// Konwerter konwertujący null na Collapsed, a nie-null na Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
