using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitGui.Core.Models;
using GitGui.Core.Services;

namespace GitGui.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly RepositoryService _repositoryService;

    public ObservableCollection<CommitModel> Commits { get; } = [];
    public ObservableCollection<DiffResultModel> SelectedCommitFiles { get; } = [];

    [ObservableProperty]
    private CommitModel? _selectedCommit;

    [ObservableProperty]
    private DiffResultModel? _selectedFileDiff;

    [ObservableProperty]
    private string? _errorMessage;

    public HistoryViewModel(RepositoryService repositoryService)
    {
        _repositoryService = repositoryService;
    }

    [RelayCommand]
    public void Refresh()
    {
        Commits.Clear();
        SelectedCommitFiles.Clear();
        SelectedFileDiff = null;
        if (!_repositoryService.IsOpen) return;

        try
        {
            foreach (var commit in _repositoryService.GetCommitLog())
            {
                Commits.Add(commit);
            }
            SelectedCommit = Commits.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnSelectedCommitChanged(CommitModel? value)
    {
        SelectedCommitFiles.Clear();
        SelectedFileDiff = null;
        if (value is null || !_repositoryService.IsOpen) return;

        try
        {
            foreach (var diff in _repositoryService.GetCommitDiff(value.Sha))
            {
                SelectedCommitFiles.Add(diff);
            }
            SelectedFileDiff = SelectedCommitFiles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void SelectFile(DiffResultModel? diff)
    {
        if (diff is null) return;
        SelectedFileDiff = diff;
    }
}
