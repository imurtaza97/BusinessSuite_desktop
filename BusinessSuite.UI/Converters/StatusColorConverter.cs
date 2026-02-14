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
                "shipped" => SolidColorBrush.Parse("#E8F5E9"),
                "received" => SolidColorBrush.Parse("#E8F5E9"),
                "unpaid" => SolidColorBrush.Parse("#FFF9C4"),
                "pending" => SolidColorBrush.Parse("#FFF9C4"),
                "partially paid" => SolidColorBrush.Parse("#FFF3E0"),
                "returned" => SolidColorBrush.Parse("#F3E5F5"),
                "returned-to-vendor" => SolidColorBrush.Parse("#F3E5F5"),
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
