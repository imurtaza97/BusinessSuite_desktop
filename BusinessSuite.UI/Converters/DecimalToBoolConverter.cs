using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BusinessSuite.UI.Converters;

public class DecimalToBoolConverter : IValueConverter
{
    public static readonly DecimalToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d > 0;
        if (value is int i)
        {
            if (parameter is string pStr && int.TryParse(pStr, out int pInt))
                return i != pInt;
            return i > 0;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
