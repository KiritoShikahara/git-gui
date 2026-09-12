namespace GitGui.Core.Models;

public sealed class CommitModel
{
    public required string Sha { get; init; }
    public required string ShortSha { get; init; }
    public required string Message { get; init; }
    public required string MessageShort { get; init; }
    public required string AuthorName { get; init; }
    public required string AuthorEmail { get; init; }
    public required DateTimeOffset When { get; init; }
    public required IReadOnlyList<string> ParentShas { get; init; }
    public required IReadOnlyList<string> LocalBranchNames { get; init; }
    public required IReadOnlyList<string> RemoteBranchNames { get; init; }
    public required IReadOnlyList<string> TagNames { get; init; }

    /// <summary>Lane index assigned by CommitGraphBuilder for graph rendering.</summary>
    public int Lane { get; set; }

    /// <summary>Lane indices of parent commits, for drawing connector lines.</summary>
    public IReadOnlyList<int> ParentLanes { get; set; } = Array.Empty<int>();

    /// <summary>True if an earlier (newer) commit in the log pointed to this commit as a parent.</summary>
    public bool HasIncomingEdge { get; set; }

    /// <summary>Lanes of other, unrelated branches that simply pass straight through this row.</summary>
    public IReadOnlyList<int> PassThroughLanes { get; set; } = Array.Empty<int>();
}
