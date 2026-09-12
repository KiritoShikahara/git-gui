using System.Text.RegularExpressions;
using GitGui.Core.Models;

namespace GitGui.Core.Services;

/// <summary>Parses a unified diff (as produced by LibGit2Sharp's PatchEntryChanges.Patch) into structured hunks/lines.</summary>
internal static partial class DiffParser
{
    [GeneratedRegex(@"^@@ -(?<oldStart>\d+)(,(?<oldCount>\d+))? \+(?<newStart>\d+)(,(?<newCount>\d+))? @@(?<header>.*)$")]
    private static partial Regex HunkHeaderRegex();

    public static IReadOnlyList<DiffHunkModel> ParseHunks(string unifiedDiffText)
    {
        var hunks = new List<DiffHunkModel>();
        if (string.IsNullOrEmpty(unifiedDiffText))
        {
            return hunks;
        }

        var lines = unifiedDiffText.Split('\n');
        List<DiffLineModel>? currentLines = null;
        string? currentHeader = null;
        int oldLine = 0;
        int newLine = 0;

        void FlushHunk()
        {
            if (currentLines is not null && currentHeader is not null)
            {
                hunks.Add(new DiffHunkModel { Header = currentHeader, Lines = currentLines });
            }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Skip the file-level headers (diff --git, index, ---, +++)
            if (line.StartsWith("diff --git", StringComparison.Ordinal) ||
                line.StartsWith("index ", StringComparison.Ordinal) ||
                line.StartsWith("--- ", StringComparison.Ordinal) ||
                line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                continue;
            }

            var match = HunkHeaderRegex().Match(line);
            if (match.Success)
            {
                FlushHunk();
                currentLines = [];
                currentHeader = line;
                oldLine = int.Parse(match.Groups["oldStart"].Value);
                newLine = int.Parse(match.Groups["newStart"].Value);
                continue;
            }

            if (currentLines is null)
            {
                // Content before any hunk header (shouldn't normally happen) - ignore.
                continue;
            }

            if (line.Length == 0)
            {
                currentLines.Add(new DiffLineModel { Kind = DiffLineKind.Context, Content = string.Empty, OldLineNumber = oldLine, NewLineNumber = newLine });
                oldLine++;
                newLine++;
                continue;
            }

            switch (line[0])
            {
                case '+':
                    currentLines.Add(new DiffLineModel { Kind = DiffLineKind.Added, Content = line[1..], NewLineNumber = newLine });
                    newLine++;
                    break;
                case '-':
                    currentLines.Add(new DiffLineModel { Kind = DiffLineKind.Removed, Content = line[1..], OldLineNumber = oldLine });
                    oldLine++;
                    break;
                case '\\':
                    currentLines.Add(new DiffLineModel { Kind = DiffLineKind.NoNewlineAtEndOfFile, Content = line });
                    break;
                case ' ':
                    currentLines.Add(new DiffLineModel { Kind = DiffLineKind.Context, Content = line[1..], OldLineNumber = oldLine, NewLineNumber = newLine });
                    oldLine++;
                    newLine++;
                    break;
                default:
                    currentLines.Add(new DiffLineModel { Kind = DiffLineKind.Context, Content = line, OldLineNumber = oldLine, NewLineNumber = newLine });
                    oldLine++;
                    newLine++;
                    break;
            }
        }

        FlushHunk();
        return hunks;
    }
}
