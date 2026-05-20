using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BioIMA.Avalonia.Converters;

public class ShapeTypeEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var shapeType = value?.ToString();
        var targetTypeName = parameter?.ToString();

        return string.Equals(shapeType, targetTypeName, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}