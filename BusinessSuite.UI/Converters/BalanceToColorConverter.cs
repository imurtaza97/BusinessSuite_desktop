using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BusinessSuite.UI.Converters;

public class BalanceToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal balance)
        {
            if (balance > 0) return Brushes.Red; // Owed/Outstanding
            if (balance < 0) return Brushes.Green; // Credit
            return Brushes.Gray; // Zero
        }
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
