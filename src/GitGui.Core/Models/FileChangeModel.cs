namespace GitGui.Core.Models;

public enum FileChangeKind
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Untracked,
    Conflicted,
    TypeChanged,
}

public sealed class FileChangeModel
{
    public required string Path { get; init; }
    public string? OldPath { get; init; }
    public required FileChangeKind Kind { get; init; }
    public required bool IsStaged { get; init; }
}
