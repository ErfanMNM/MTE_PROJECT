using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace CodePlus.Views;

public sealed class RoleAlignmentConverter : IValueConverter
{
    public static readonly RoleAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value as string == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class RoleBrushConverter : IValueConverter
{
    public static readonly RoleBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Avalonia.Application.Current;
        if (app is null) return Brushes.Gray;
        return value as string == "user"
            ? app.Resources["PrimaryHueMidBrush"]
            : app.Resources["MaterialDesignCardBackground"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
