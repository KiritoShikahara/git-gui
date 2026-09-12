using System.Windows;
using System.Windows.Media;
using GitGui.Core.Models;

namespace GitGui.App.Controls;

/// <summary>Draws one row of the simplified commit graph (dot + connector lines) for a single commit.</summary>
public sealed class CommitGraphCell : FrameworkElement
{
    private const double LaneWidth = 16.0;
    private const double DotRadius = 4.0;

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

    public static readonly DependencyProperty CommitProperty = DependencyProperty.Register(
        nameof(Commit), typeof(CommitModel), typeof(CommitGraphCell),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnCommitChanged));

    public CommitModel? Commit
    {
        get => (CommitModel?)GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    public CommitGraphCell()
    {
        Width = 8 * LaneWidth;
        ClipToBounds = false;
    }

    private static void OnCommitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CommitGraphCell)d).InvalidateVisual();

    private static Pen PenFor(int lane)
    {
        var color = Palette[((lane % Palette.Length) + Palette.Length) % Palette.Length];
        var pen = new Pen(new SolidColorBrush(color), 2.0);
        pen.Freeze();
        return pen;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var commit = Commit;
        if (commit is null) return;

        double height = ActualHeight > 0 ? ActualHeight : 22;
        double midY = height / 2;

        static double X(int lane) => lane * LaneWidth + LaneWidth / 2;

        // Straight through-lines for unrelated branches passing through this row.
        foreach (var lane in commit.PassThroughLanes)
        {
            var x = X(lane);
            dc.DrawLine(PenFor(lane), new Point(x, 0), new Point(x, height));
        }

        var ownX = X(commit.Lane);
        var ownPen = PenFor(commit.Lane);

        if (commit.HasIncomingEdge)
        {
            dc.DrawLine(ownPen, new Point(ownX, 0), new Point(ownX, midY));
        }

        foreach (var parentLane in commit.ParentLanes)
        {
            var targetX = X(parentLane);
            var pen = parentLane == commit.Lane ? ownPen : PenFor(parentLane);
            dc.DrawLine(pen, new Point(ownX, midY), new Point(targetX, height));
        }

        var dotBrush = new SolidColorBrush(Palette[((commit.Lane % Palette.Length) + Palette.Length) % Palette.Length]);
        dc.DrawEllipse(dotBrush, null, new Point(ownX, midY), DotRadius, DotRadius);
    }
}
