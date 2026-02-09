using System;
using System.Globalization;
using System.Windows.Data;

namespace ERP.UI.WPF.Converters;

/// <summary>
/// Konwerter zwracający true, gdy wartość nie jest null.
/// </summary>
public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
