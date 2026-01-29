using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BusinessSuite.UI.Converters;

public class StatusBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        return status.ToLower() switch
        {
            "paid" => Brush.Parse("#DCFCE7"),
            "unpaid" => Brush.Parse("#FEE2E2"),
            "pending" => Brush.Parse("#FEF3C7"),
            "cancelled" => Brush.Parse("#F3F4F6"),
            _ => Brush.Parse("#F3F4F6")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusFgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        return status.ToLower() switch
        {
            "paid" => Brush.Parse("#166534"),
            "unpaid" => Brush.Parse("#991B1B"),
            "pending" => Brush.Parse("#92400E"),
            "cancelled" => Brush.Parse("#4B5563"),
            _ => Brush.Parse("#4B5563")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TransactionTypeBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string type = value?.ToString() ?? "";
        return type.ToLower() switch
        {
            "sale" => Brush.Parse("#2563EB"),
            "purchase" => Brush.Parse("#7C3AED"),
            _ => Brush.Parse("#9CA3AF")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
