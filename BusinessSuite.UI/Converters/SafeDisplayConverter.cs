using System;
using System.Globalization;
using Avalonia.Data.Converters;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.UI.Converters;

public class SafeDisplayConverter : IValueConverter
{
    public static readonly SafeDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value == null)
            {
                if (parameter is string paramStr)
                {
                    if (paramStr == "Customer") return "(No Customer)";
                    if (paramStr == "Date") return "";
                }
                return "-";
            }

            // Handle Customer entity directly
            if (value is Customer customer)
            {
                return customer.CustomerName ?? "(No Name)";
            }

            // Handle string formatting if parameter is provided
            if (parameter is string format && !string.IsNullOrEmpty(format))
            {
                // Unescape definition if passed from XAML like {}...
                if (format.StartsWith("{}")) format = format.Substring(2);
                
                return string.Format(culture, format, value);
            }

            return value;
        }
        catch (Exception)
        {
            // Fail safe
            return "Error";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
