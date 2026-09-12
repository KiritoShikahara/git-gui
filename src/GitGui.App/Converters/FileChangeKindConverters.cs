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
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FileChangeKind.Added => Brushes.SeaGreen,
        FileChangeKind.Modified => Brushes.DarkGoldenrod,
        FileChangeKind.Deleted => Brushes.IndianRed,
        FileChangeKind.Renamed => Brushes.SteelBlue,
        FileChangeKind.Untracked => Brushes.Gray,
        FileChangeKind.Conflicted => Brushes.Red,
        FileChangeKind.TypeChanged => Brushes.Purple,
        _ => Brushes.Black,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
