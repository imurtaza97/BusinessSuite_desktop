using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BusinessSuite.UI.Converters;

public class BoolToColumnSpanConverter : IValueConverter
{
    public static readonly BoolToColumnSpanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? 2 : 1;
        return 1;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
