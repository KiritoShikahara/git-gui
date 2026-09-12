namespace GitGui.Core.Models;

public sealed class BranchModel
{
    public required string Name { get; init; }
    public required string FriendlyName { get; init; }
    public required bool IsRemote { get; init; }
    public required bool IsCurrentRepositoryHead { get; init; }
    public string? UpstreamName { get; init; }
    public int AheadBy { get; init; }
    public int BehindBy { get; init; }
    public string? TipSha { get; init; }
}
