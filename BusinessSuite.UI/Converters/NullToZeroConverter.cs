using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BusinessSuite.UI.Converters;

public class NullToZeroConverter : IValueConverter
{
    public static readonly NullToZeroConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return 0m;
        
        if (value is decimal d)
            return d;

        if (decimal.TryParse(value.ToString(), out var result))
            return result;

        return 0m;
    }
}
