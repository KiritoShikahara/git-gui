using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GitGui.Core.Models;

namespace GitGui.App.Converters;

public sealed class DiffLineKindToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush Added = new(Color.FromRgb(0xDD, 0xF4, 0xDD));
    private static readonly SolidColorBrush Removed = new(Color.FromRgb(0xFB, 0xDD, 0xDD));
    private static readonly SolidColorBrush Header = new(Color.FromRgb(0xE9, 0xE9, 0xF2));
    private static readonly SolidColorBrush Transparent = Brushes.Transparent;

    static DiffLineKindToBackgroundConverter()
    {
        Added.Freeze();
        Removed.Freeze();
        Header.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        DiffLineKind.Added => Added,
        DiffLineKind.Removed => Removed,
        DiffLineKind.Header => Header,
        _ => Transparent,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class DiffLineKindToPrefixConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        DiffLineKind.Added => "+",
        DiffLineKind.Removed => "-",
        _ => " ",
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class LaneToColorConverter : IValueConverter
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(0x3B, 0x82, 0xF6),
        Color.FromRgb(0xEF, 0x44, 0x44),
        Color.FromRgb(0x22, 0xC5, 0x5E),
        Color.FromRgb(0xF5, 0x9E, 0x0B),
        Color.FromRgb(0xA8, 0x55, 0xF7),
        Color.FromRgb(0x06, 0xB6, 0xD4),
        Color.FromRgb(0xEC, 0x48, 0x99),
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int lane = value is int i ? i : 0;
        var color = Palette[((lane % Palette.Length) + Palette.Length) % Palette.Length];
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
