using GitGui.Core.Models;
using LibGit2Sharp;

namespace GitGui.Core.Services;

/// <summary>
/// Wraps LibGit2Sharp to provide the repository-local git operations used by the app
/// (status, staging, commit, diff, branches, log). Remote network operations (fetch/pull/push)
/// are handled separately by <see cref="GitCliService"/> so the user's system git credentials apply.
/// </summary>
public sealed class RepositoryService : IDisposable
{
    private Repository? _repo;

    public string? RepositoryRoot { get; private set; }

    public bool IsOpen => _repo is not null;

    public static string? DiscoverRepository(string path) => Repository.Discover(path);

    public void Open(string path)
    {
        var discovered = Repository.Discover(path) ?? throw new InvalidOperationException($"'{path}' はGitリポジトリではありません。");
        _repo?.Dispose();
        _repo = new Repository(discovered);
        RepositoryRoot = _repo.Info.WorkingDirectory;
    }

    public void Init(string path)
    {
        Repository.Init(path);
        Open(path);
    }

    public void Close()
    {
        _repo?.Dispose();
        _repo = null;
        RepositoryRoot = null;
    }

    public void Dispose() => Close();

    private Repository Repo => _repo ?? throw new InvalidOperationException("リポジトリが開かれていません。");

    public string? CurrentBranchFriendlyName => Repo.Head.FriendlyName;

    public bool IsHeadDetached => Repo.Info.IsHeadDetached;

    // ---------------- Status / Staging ----------------

    public IReadOnlyList<FileChangeModel> GetStatus()
    {
        var repo = Repo;
        var result = new List<FileChangeModel>();
        var options = new StatusOptions
        {
            IncludeUntracked = true,
            RecurseUntrackedDirs = true,
            RecurseIgnoredDirs = false,
        };

        foreach (var entry in repo.RetrieveStatus(options))
        {
            var state = entry.State;
            if (state.HasFlag(FileStatus.Ignored) || state == FileStatus.Unaltered)
            {
                continue;
            }

            if (state.HasFlag(FileStatus.Conflicted))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Conflicted, IsStaged = false });
                continue;
            }

            // Staged (index vs HEAD) changes.
            if (state.HasFlag(FileStatus.NewInIndex))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Added, IsStaged = true });
            }
            if (state.HasFlag(FileStatus.ModifiedInIndex))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Modified, IsStaged = true });
            }
            if (state.HasFlag(FileStatus.DeletedFromIndex))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Deleted, IsStaged = true });
            }
            if (state.HasFlag(FileStatus.RenamedInIndex))
            {
                result.Add(new FileChangeModel
                {
                    Path = entry.FilePath,
                    OldPath = entry.HeadToIndexRenameDetails?.OldFilePath,
                    Kind = FileChangeKind.Renamed,
                    IsStaged = true,
                });
            }
            if (state.HasFlag(FileStatus.TypeChangeInIndex))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.TypeChanged, IsStaged = true });
            }

            // Unstaged (workdir vs index) changes.
            if (state.HasFlag(FileStatus.NewInWorkdir))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Untracked, IsStaged = false });
            }
            if (state.HasFlag(FileStatus.ModifiedInWorkdir))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Modified, IsStaged = false });
            }
            if (state.HasFlag(FileStatus.DeletedFromWorkdir))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.Deleted, IsStaged = false });
            }
            if (state.HasFlag(FileStatus.RenamedInWorkdir))
            {
                result.Add(new FileChangeModel
                {
                    Path = entry.FilePath,
                    OldPath = entry.IndexToWorkDirRenameDetails?.OldFilePath,
                    Kind = FileChangeKind.Renamed,
                    IsStaged = false,
                });
            }
            if (state.HasFlag(FileStatus.TypeChangeInWorkdir))
            {
                result.Add(new FileChangeModel { Path = entry.FilePath, Kind = FileChangeKind.TypeChanged, IsStaged = false });
            }
        }

        return result;
    }

    public void Stage(string relativePath) => Commands.Stage(Repo, relativePath);

    public void StageAll(IEnumerable<string> relativePaths) => Commands.Stage(Repo, relativePaths);

    public void Unstage(string relativePath) => Commands.Unstage(Repo, relativePath);

    public void UnstageAll(IEnumerable<string> relativePaths) => Commands.Unstage(Repo, relativePaths);

    /// <summary>Discards working-directory changes for a path, restoring it to the last committed (HEAD) content.
    /// For untracked files this deletes the file instead.</summary>
    public void DiscardChanges(string relativePath, bool isUntracked)
    {
        var repo = Repo;
        if (isUntracked)
        {
            var fullPath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return;
        }

        if (repo.Head.Tip is null)
        {
            return;
        }

        repo.CheckoutPaths(repo.Head.Tip.Sha, [relativePath], new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
    }

    // ---------------- Commit ----------------

    public CommitModel Commit(string message, bool amend)
    {
        var repo = Repo;
        Signature signature = BuildSignature(repo);
        var commit = repo.Commit(message, signature, signature, new CommitOptions { AmendPreviousCommit = amend });
        return ToCommitModel(commit);
    }

    private static Signature BuildSignature(Repository repo)
    {
        var config = repo.Config;
        var name = config.Get<string>("user.name")?.Value ?? "Unknown";
        var email = config.Get<string>("user.email")?.Value ?? "unknown@example.com";
        return new Signature(name, email, DateTimeOffset.Now);
    }

    // ---------------- Identity ----------------

    /// <summary>The name/email set specifically at the repository level (not global), or null if unset there.</summary>
    public GitIdentityModel? GetLocalIdentity()
    {
        var config = Repo.Config;
        var name = config.Get<string>("user.name", ConfigurationLevel.Local)?.Value;
        var email = config.Get<string>("user.email", ConfigurationLevel.Local)?.Value;
        return name is not null && email is not null ? new GitIdentityModel { Name = name, Email = email } : null;
    }

    /// <summary>The name/email that would actually be used for the next commit (local, falling back to global/system).</summary>
    public GitIdentityModel GetEffectiveIdentity()
    {
        var config = Repo.Config;
        var name = config.Get<string>("user.name")?.Value ?? "Unknown";
        var email = config.Get<string>("user.email")?.Value ?? "unknown@example.com";
        return new GitIdentityModel { Name = name, Email = email };
    }

    /// <summary>Sets user.name/user.email in this repository's local config, overriding the global identity for this repo only.</summary>
    public void SetLocalIdentity(string name, string email)
    {
        var repo = Repo;
        repo.Config.Set("user.name", name, ConfigurationLevel.Local);
        repo.Config.Set("user.email", email, ConfigurationLevel.Local);
    }

    // ---------------- Diff ----------------

    public DiffResultModel GetFileDiff(string relativePath, bool staged)
    {
        var repo = Repo;
        Patch patch;

        if (staged)
        {
            var headTree = repo.Head.Tip?.Tree;
            patch = repo.Diff.Compare<Patch>(headTree, DiffTargets.Index, [relativePath]);
        }
        else
        {
            patch = repo.Diff.Compare<Patch>([relativePath], includeUntracked: true);
        }

        var entry = patch.FirstOrDefault(e => e.Path == relativePath || e.OldPath == relativePath);
        if (entry is null)
        {
            return new DiffResultModel { Path = relativePath, IsBinary = false, Hunks = [] };
        }

        return new DiffResultModel
        {
            Path = entry.Path,
            OldPath = entry.OldPath == entry.Path ? null : entry.OldPath,
            IsBinary = entry.IsBinaryComparison,
            Hunks = entry.IsBinaryComparison ? [] : DiffParser.ParseHunks(entry.Patch),
        };
    }

    public IReadOnlyList<DiffResultModel> GetCommitDiff(string sha)
    {
        var repo = Repo;
        var commit = repo.Lookup<Commit>(sha) ?? throw new InvalidOperationException($"コミット {sha} が見つかりません。");
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;
        var patch = repo.Diff.Compare<Patch>(parentTree, commit.Tree);

        return patch.Select(entry => new DiffResultModel
        {
            Path = entry.Path,
            OldPath = entry.OldPath == entry.Path ? null : entry.OldPath,
            IsBinary = entry.IsBinaryComparison,
            Hunks = entry.IsBinaryComparison ? [] : DiffParser.ParseHunks(entry.Patch),
        }).ToList();
    }

    // ---------------- Branches ----------------

    public IReadOnlyList<BranchModel> GetBranches()
    {
        var repo = Repo;
        return repo.Branches.Select(b => new BranchModel
        {
            Name = b.CanonicalName,
            FriendlyName = b.FriendlyName,
            IsRemote = b.IsRemote,
            IsCurrentRepositoryHead = b.IsCurrentRepositoryHead,
            UpstreamName = b.TrackedBranch?.FriendlyName,
            AheadBy = b.TrackingDetails.AheadBy ?? 0,
            BehindBy = b.TrackingDetails.BehindBy ?? 0,
            TipSha = b.Tip?.Sha,
        }).ToList();
    }

    public void CreateBranch(string name, string? startPointSha = null)
    {
        var repo = Repo;
        if (startPointSha is not null)
        {
            var commit = repo.Lookup<Commit>(startPointSha) ?? throw new InvalidOperationException("開始コミットが見つかりません。");
            repo.CreateBranch(name, commit);
        }
        else
        {
            repo.CreateBranch(name);
        }
    }

    public void CheckoutBranch(string branchNameOrCanonicalName)
    {
        var repo = Repo;
        var branch = repo.Branches[branchNameOrCanonicalName] ?? throw new InvalidOperationException($"ブランチ {branchNameOrCanonicalName} が見つかりません。");
        Commands.Checkout(repo, branch);
    }

    public void DeleteBranch(string branchNameOrCanonicalName, bool force = false)
    {
        var repo = Repo;
        var branch = repo.Branches[branchNameOrCanonicalName] ?? throw new InvalidOperationException($"ブランチ {branchNameOrCanonicalName} が見つかりません。");
        repo.Branches.Remove(branch);
    }

    public MergeStatus MergeBranchIntoCurrent(string branchNameOrCanonicalName)
    {
        var repo = Repo;
        var branch = repo.Branches[branchNameOrCanonicalName] ?? throw new InvalidOperationException($"ブランチ {branchNameOrCanonicalName} が見つかりません。");
        var signature = BuildSignature(repo);
        var result = repo.Merge(branch, signature, new MergeOptions());
        return result.Status;
    }

    // ---------------- Log ----------------

    public IReadOnlyList<CommitModel> GetCommitLog(string? branchCanonicalName = null, int maxCount = 500)
    {
        var repo = Repo;
        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
        };

        if (branchCanonicalName is not null)
        {
            var branch = repo.Branches[branchCanonicalName];
            if (branch is not null)
            {
                filter.IncludeReachableFrom = branch;
            }
        }

        var refsByTarget = BuildRefLookup(repo);

        var commits = repo.Commits.QueryBy(filter)
            .Take(maxCount)
            .Select(c => ToCommitModel(c, refsByTarget))
            .ToList();

        CommitGraphBuilder.AssignLanes(commits);
        return commits;
    }

    private static Dictionary<string, (List<string> Local, List<string> Remote, List<string> Tags)> BuildRefLookup(Repository repo)
    {
        var map = new Dictionary<string, (List<string> Local, List<string> Remote, List<string> Tags)>();

        (List<string> Local, List<string> Remote, List<string> Tags) GetOrAdd(string sha)
        {
            if (!map.TryGetValue(sha, out var lists))
            {
                lists = ([], [], []);
                map[sha] = lists;
            }
            return lists;
        }

        foreach (var branch in repo.Branches)
        {
            if (branch.Tip is null) continue;
            var lists = GetOrAdd(branch.Tip.Sha);
            (branch.IsRemote ? lists.Remote : lists.Local).Add(branch.FriendlyName);
        }

        foreach (var tag in repo.Tags)
        {
            if (tag.Target is null) continue;
            GetOrAdd(tag.Target.Sha).Tags.Add(tag.FriendlyName);
        }

        return map;
    }

    private static CommitModel ToCommitModel(Commit commit) => ToCommitModel(commit, new Dictionary<string, (List<string>, List<string>, List<string>)>());

    private static CommitModel ToCommitModel(Commit commit, Dictionary<string, (List<string> Local, List<string> Remote, List<string> Tags)> refsByTarget)
    {
        refsByTarget.TryGetValue(commit.Sha, out var refs);

        return new CommitModel
        {
            Sha = commit.Sha,
            ShortSha = commit.Sha[..7],
            Message = commit.Message,
            MessageShort = commit.MessageShort,
            AuthorName = commit.Author.Name,
            AuthorEmail = commit.Author.Email,
            When = commit.Author.When,
            ParentShas = commit.Parents.Select(p => p.Sha).ToList(),
            LocalBranchNames = refs.Local ?? [],
            RemoteBranchNames = refs.Remote ?? [],
            TagNames = refs.Tags ?? [],
        };
    }
}
