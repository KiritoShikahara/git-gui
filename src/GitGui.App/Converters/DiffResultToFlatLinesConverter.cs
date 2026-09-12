using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GitGui.Core.Models;

namespace GitGui.App.Converters;

/// <summary>Flattens a DiffResultModel's hunks into a single line sequence (with hunk headers as pseudo-lines) for display.</summary>
public sealed class DiffResultToFlatLinesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DiffResultModel diff)
        {
            return Array.Empty<DiffLineModel>();
        }

        if (diff.IsBinary)
        {
            return new[] { new DiffLineModel { Kind = DiffLineKind.Header, Content = "(バイナリファイル)" } };
        }

        var lines = new List<DiffLineModel>();
        foreach (var hunk in diff.Hunks)
        {
            lines.Add(new DiffLineModel { Kind = DiffLineKind.Header, Content = hunk.Header });
            lines.AddRange(hunk.Lines);
        }
        return lines;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
