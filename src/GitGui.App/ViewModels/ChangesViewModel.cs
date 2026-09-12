using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitGui.Core.Models;
using GitGui.Core.Services;

namespace GitGui.App.ViewModels;

public partial class ChangesViewModel : ObservableObject
{
    private readonly RepositoryService _repositoryService;
    private readonly Action _onRepositoryChanged;

    public ObservableCollection<FileChangeModel> StagedFiles { get; } = [];
    public ObservableCollection<FileChangeModel> UnstagedFiles { get; } = [];

    [ObservableProperty]
    private FileChangeModel? _selectedFile;

    [ObservableProperty]
    private DiffResultModel? _currentDiff;

    [ObservableProperty]
    private string _commitMessage = string.Empty;

    [ObservableProperty]
    private bool _amend;

    [ObservableProperty]
    private string? _errorMessage;

    public ChangesViewModel(RepositoryService repositoryService, Action onRepositoryChanged)
    {
        _repositoryService = repositoryService;
        _onRepositoryChanged = onRepositoryChanged;
    }

    partial void OnSelectedFileChanged(FileChangeModel? value)
    {
        if (value is null || !_repositoryService.IsOpen)
        {
            CurrentDiff = null;
            return;
        }

        try
        {
            CurrentDiff = _repositoryService.GetFileDiff(value.Path, value.IsStaged);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        if (!_repositoryService.IsOpen)
        {
            StagedFiles.Clear();
            UnstagedFiles.Clear();
            return;
        }

        var previouslySelectedPath = SelectedFile?.Path;
        var status = _repositoryService.GetStatus();

        StagedFiles.Clear();
        UnstagedFiles.Clear();
        foreach (var change in status)
        {
            (change.IsStaged ? StagedFiles : UnstagedFiles).Add(change);
        }

        SelectedFile = StagedFiles.FirstOrDefault(f => f.Path == previouslySelectedPath)
                        ?? UnstagedFiles.FirstOrDefault(f => f.Path == previouslySelectedPath);

        CommitCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Stage(FileChangeModel? file)
    {
        if (file is null) return;
        RunGitAction(() => _repositoryService.Stage(file.Path));
    }

    [RelayCommand]
    private void Unstage(FileChangeModel? file)
    {
        if (file is null) return;
        RunGitAction(() => _repositoryService.Unstage(file.Path));
    }

    [RelayCommand]
    private void StageAll()
    {
        RunGitAction(() => _repositoryService.StageAll(UnstagedFiles.Select(f => f.Path).ToList()));
    }

    [RelayCommand]
    private void UnstageAll()
    {
        RunGitAction(() => _repositoryService.UnstageAll(StagedFiles.Select(f => f.Path).ToList()));
    }

    [RelayCommand]
    private void Discard(FileChangeModel? file)
    {
        if (file is null) return;
        RunGitAction(() => _repositoryService.DiscardChanges(file.Path, file.Kind == FileChangeKind.Untracked));
    }

    private bool CanCommit() => _repositoryService.IsOpen && (Amend || StagedFiles.Count > 0) && !string.IsNullOrWhiteSpace(CommitMessage);

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private void Commit()
    {
        RunGitAction(() =>
        {
            _repositoryService.Commit(CommitMessage, Amend);
            CommitMessage = string.Empty;
            Amend = false;
        });
    }

    partial void OnCommitMessageChanged(string value) => CommitCommand.NotifyCanExecuteChanged();
    partial void OnAmendChanged(bool value) => CommitCommand.NotifyCanExecuteChanged();

    private void RunGitAction(Action action)
    {
        try
        {
            ErrorMessage = null;
            action();
            Refresh();
            _onRepositoryChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
