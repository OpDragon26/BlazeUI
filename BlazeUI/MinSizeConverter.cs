using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BlazeUI;

public class MinSizeConverter : IMultiValueConverter
{
    private double margin = 20;
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not double width || values[1] is not double height)
            return 0d;

        return Math.Min(width, height) - margin * 2;
    }
}