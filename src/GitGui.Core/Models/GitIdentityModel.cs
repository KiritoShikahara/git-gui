namespace GitGui.Core.Models;

public sealed class GitIdentityModel
{
    public required string Name { get; init; }
    public required string Email { get; init; }

    public string DisplayName => $"{Name} <{Email}>";

    public override bool Equals(object? obj) =>
        obj is GitIdentityModel other && Name == other.Name && Email == other.Email;

    public override int GetHashCode() => HashCode.Combine(Name, Email);
}
