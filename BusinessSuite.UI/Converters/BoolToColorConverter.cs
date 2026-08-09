using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BusinessSuite.UI.Converters;

/// <summary>
/// Converts a bool to one of two color strings passed as ConverterParameter="falseColor|trueColor".
/// Example: ConverterParameter="#9CA3AF|#111827" → false → #9CA3AF, true → #111827
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        string param = parameter as string ?? "#9CA3AF|#111827";
        var parts = param.Split('|');
        string hex = boolValue
            ? (parts.Length > 1 ? parts[1] : "#111827")
            : parts[0];

        try { return SolidColorBrush.Parse(hex); }
        catch { return new SolidColorBrush(Colors.Gray); }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
