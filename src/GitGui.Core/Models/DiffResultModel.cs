namespace GitGui.Core.Models;

public enum DiffLineKind
{
    Context,
    Added,
    Removed,
    Header,
    NoNewlineAtEndOfFile,
}

public sealed class DiffLineModel
{
    public required DiffLineKind Kind { get; init; }
    public required string Content { get; init; }
    public int? OldLineNumber { get; init; }
    public int? NewLineNumber { get; init; }
}

public sealed class DiffHunkModel
{
    public required string Header { get; init; }
    public required IReadOnlyList<DiffLineModel> Lines { get; init; }
}

public sealed class DiffResultModel
{
    public required string Path { get; init; }
    public string? OldPath { get; init; }
    public required bool IsBinary { get; init; }
    public required IReadOnlyList<DiffHunkModel> Hunks { get; init; }
}
