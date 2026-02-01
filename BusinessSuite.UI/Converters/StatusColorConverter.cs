using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BusinessSuite.UI.Converters;

public class StatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "paid" => SolidColorBrush.Parse("#E1F5FE"),
                "unpaid" => SolidColorBrush.Parse("#FFF9C4"),
                "pending" => SolidColorBrush.Parse("#FFF9C4"),
                "overdue" => SolidColorBrush.Parse("#FFEBEE"),
                "cancelled" => SolidColorBrush.Parse("#EEEEEE"),
                _ => Brushes.Transparent
            };
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
