using System.Diagnostics;

namespace GitGui.Core.Services;

public sealed class GitOperationResult
{
    public required bool Success { get; init; }
    public required int ExitCode { get; init; }
    public required string Output { get; init; }
}

/// <summary>
/// Runs network-touching git operations (fetch/pull/push/clone) via the system's git.exe process,
/// so the user's existing credential helper (Windows Credential Manager / Git Credential Manager / SSH agent)
/// is used automatically instead of reimplementing authentication.
/// </summary>
public sealed class GitCliService
{
    public event Action<string>? OutputReceived;

    public Task<GitOperationResult> FetchAsync(string workingDirectory, string remote = "--all", CancellationToken ct = default) =>
        RunAsync(workingDirectory, $"fetch {(remote == "--all" ? "--all" : remote)} --progress", ct);

    public Task<GitOperationResult> PullAsync(string workingDirectory, string? remote = null, string? branch = null, CancellationToken ct = default) =>
        RunAsync(workingDirectory, $"pull --progress {remote} {branch}".TrimEnd(), ct);

    public Task<GitOperationResult> PushAsync(string workingDirectory, string? remote = null, string? branch = null, bool setUpstream = false, CancellationToken ct = default)
    {
        var args = "push --progress";
        if (setUpstream) args += " -u";
        if (remote is not null) args += $" {remote}";
        if (branch is not null) args += $" {branch}";
        return RunAsync(workingDirectory, args, ct);
    }

    public Task<GitOperationResult> CloneAsync(string url, string destinationPath, CancellationToken ct = default) =>
        RunAsync(Path.GetDirectoryName(destinationPath) ?? ".", $"clone --progress \"{url}\" \"{destinationPath}\"", ct);

    private async Task<GitOperationResult> RunAsync(string workingDirectory, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var outputLines = new List<string>();

        void OnLine(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;
            outputLines.Add(e.Data);
            OutputReceived?.Invoke(e.Data);
        }

        process.OutputDataReceived += OnLine;
        process.ErrorDataReceived += OnLine;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return new GitOperationResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            Output = string.Join(Environment.NewLine, outputLines),
        };
    }
}
