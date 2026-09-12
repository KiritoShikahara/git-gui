using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GitGui.Core.Models;

namespace GitGui.App.Converters;

public sealed class FileChangeKindToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FileChangeKind.Added => "A",
        FileChangeKind.Modified => "M",
        FileChangeKind.Deleted => "D",
        FileChangeKind.Renamed => "R",
        FileChangeKind.Untracked => "U",
        FileChangeKind.Conflicted => "!",
        FileChangeKind.TypeChanged => "T",
        _ => "?",
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class FileChangeKindToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Added = Freeze(Color.FromRgb(0x3F, 0xB9, 0x50));
    private static readonly SolidColorBrush Modified = Freeze(Color.FromRgb(0xD2, 0x99, 0x22));
    private static readonly SolidColorBrush Deleted = Freeze(Color.FromRgb(0xF8, 0x51, 0x49));
    private static readonly SolidColorBrush Renamed = Freeze(Color.FromRgb(0x58, 0xA6, 0xFF));
    private static readonly SolidColorBrush Untracked = Freeze(Color.FromRgb(0x8B, 0x94, 0x9E));
    private static readonly SolidColorBrush Conflicted = Freeze(Color.FromRgb(0xF8, 0x51, 0x49));
    private static readonly SolidColorBrush TypeChanged = Freeze(Color.FromRgb(0xA3, 0x71, 0xF7));

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FileChangeKind.Added => Added,
        FileChangeKind.Modified => Modified,
        FileChangeKind.Deleted => Deleted,
        FileChangeKind.Renamed => Renamed,
        FileChangeKind.Untracked => Untracked,
        FileChangeKind.Conflicted => Conflicted,
        FileChangeKind.TypeChanged => TypeChanged,
        _ => Brushes.Gray,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
