using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitGui.Core.Models;
using GitGui.Core.Services;

namespace GitGui.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly RepositoryService _repositoryService = new();
    private readonly GitCliService _gitCliService = new();
    private readonly IdentityStore _identityStore = new();
    private bool _suppressIdentityApply;

    public ChangesViewModel ChangesVm { get; }
    public BranchesViewModel BranchesVm { get; }
    public HistoryViewModel HistoryVm { get; }

    public ObservableCollection<GitIdentityModel> Identities { get; } = [];

    [ObservableProperty]
    private GitIdentityModel? _selectedIdentity;

    [ObservableProperty]
    private string _repositoryPath = "リポジトリが開かれていません";

    [ObservableProperty]
    private string _currentBranchName = "-";

    [ObservableProperty]
    private string _statusMessage = "「開く」からGitリポジトリを選択してください。";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRepositoryOpen;

    [ObservableProperty]
    private int _selectedTabIndex;

    public event Func<string?>? RequestFolderPick;
    public event Func<(string Url, string Destination)?>? RequestCloneDialog;
    public event Func<IReadOnlyList<GitIdentityModel>, IReadOnlyList<GitIdentityModel>>? RequestManageIdentities;

    public MainWindowViewModel()
    {
        ChangesVm = new ChangesViewModel(_repositoryService, OnRepositoryChanged);
        BranchesVm = new BranchesViewModel(_repositoryService, OnRepositoryChanged);
        HistoryVm = new HistoryViewModel(_repositoryService);
        _gitCliService.OutputReceived += line => App.Current.Dispatcher.Invoke(() => StatusMessage = line);

        foreach (var identity in _identityStore.Load())
        {
            Identities.Add(identity);
        }
    }

    partial void OnSelectedIdentityChanged(GitIdentityModel? value)
    {
        if (_suppressIdentityApply || value is null || !IsRepositoryOpen) return;

        _repositoryService.SetLocalIdentity(value.Name, value.Email);
        StatusMessage = $"このリポジトリのアカウントを「{value.DisplayName}」に切り替えました。";
    }

    [RelayCommand]
    private void ManageIdentities()
    {
        var result = RequestManageIdentities?.Invoke(Identities.ToList());
        if (result is null) return;

        var previousSelection = SelectedIdentity;
        Identities.Clear();
        foreach (var identity in result)
        {
            Identities.Add(identity);
        }
        _identityStore.Save(Identities);

        if (previousSelection is not null && Identities.Contains(previousSelection))
        {
            _suppressIdentityApply = true;
            SelectedIdentity = Identities.First(i => i.Equals(previousSelection));
            _suppressIdentityApply = false;
        }
        else
        {
            SelectedIdentity = null;
        }
    }

    /// <summary>Syncs the account dropdown to whichever identity is actually configured for this repository,
    /// registering it as a new saved identity the first time it's seen.</summary>
    private void SyncIdentityFromRepository()
    {
        var effective = _repositoryService.GetEffectiveIdentity();
        var match = Identities.FirstOrDefault(i => i.Equals(effective));
        if (match is null)
        {
            match = effective;
            Identities.Add(match);
            _identityStore.Save(Identities);
        }

        _suppressIdentityApply = true;
        SelectedIdentity = match;
        _suppressIdentityApply = false;
    }

    private void OnRepositoryChanged()
    {
        RefreshBranchInfo();
        HistoryVm.Refresh();
    }

    public void OpenRepository(string path)
    {
        try
        {
            _repositoryService.Open(path);
            RepositoryPath = _repositoryService.RepositoryRoot ?? path;
            IsRepositoryOpen = true;
            RefreshAll();
            SyncIdentityFromRepository();
            StatusMessage = "リポジトリを開きました。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshAll()
    {
        RefreshBranchInfo();
        ChangesVm.Refresh();
        BranchesVm.Refresh();
        HistoryVm.Refresh();
    }

    private void RefreshBranchInfo()
    {
        if (!_repositoryService.IsOpen)
        {
            CurrentBranchName = "-";
            return;
        }
        CurrentBranchName = _repositoryService.IsHeadDetached
            ? "(detached)"
            : _repositoryService.CurrentBranchFriendlyName ?? "-";
    }

    [RelayCommand]
    private async Task Fetch()
    {
        if (!IsRepositoryOpen) return;
        await RunRemoteOperation(() => _gitCliService.FetchAsync(RepositoryPath));
    }

    [RelayCommand]
    private async Task Pull()
    {
        if (!IsRepositoryOpen) return;
        await RunRemoteOperation(() => _gitCliService.PullAsync(RepositoryPath));
    }

    [RelayCommand]
    private async Task Push()
    {
        if (!IsRepositoryOpen) return;
        await RunRemoteOperation(() => _gitCliService.PushAsync(RepositoryPath));
    }

    private async Task RunRemoteOperation(Func<Task<GitOperationResult>> operation)
    {
        IsBusy = true;
        try
        {
            var result = await operation();
            StatusMessage = result.Success ? "完了しました。" : $"失敗しました (終了コード {result.ExitCode})";
            RefreshAll();
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenRepositoryDialog()
    {
        var path = RequestFolderPick?.Invoke();
        if (!string.IsNullOrWhiteSpace(path))
        {
            OpenRepository(path);
        }
    }

    [RelayCommand]
    private async Task CloneRepositoryDialog()
    {
        var input = RequestCloneDialog?.Invoke();
        if (input is null) return;

        var (url, destination) = input.Value;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(destination)) return;

        IsBusy = true;
        StatusMessage = "クローン中...";
        try
        {
            var result = await _gitCliService.CloneAsync(url, destination);
            if (result.Success)
            {
                OpenRepository(destination);
            }
            else
            {
                StatusMessage = $"クローンに失敗しました (終了コード {result.ExitCode})";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
