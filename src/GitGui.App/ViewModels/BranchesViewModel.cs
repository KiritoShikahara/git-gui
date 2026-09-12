using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitGui.Core.Models;
using GitGui.Core.Services;

namespace GitGui.App.ViewModels;

public partial class BranchesViewModel : ObservableObject
{
    private readonly RepositoryService _repositoryService;
    private readonly Action _onRepositoryChanged;

    public ObservableCollection<BranchModel> Branches { get; } = [];

    [ObservableProperty]
    private BranchModel? _selectedBranch;

    [ObservableProperty]
    private string _newBranchName = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public BranchesViewModel(RepositoryService repositoryService, Action onRepositoryChanged)
    {
        _repositoryService = repositoryService;
        _onRepositoryChanged = onRepositoryChanged;
    }

    [RelayCommand]
    public void Refresh()
    {
        Branches.Clear();
        if (!_repositoryService.IsOpen) return;

        foreach (var branch in _repositoryService.GetBranches().OrderByDescending(b => b.IsCurrentRepositoryHead).ThenBy(b => b.IsRemote).ThenBy(b => b.FriendlyName))
        {
            Branches.Add(branch);
        }
    }

    [RelayCommand]
    private void Checkout(BranchModel? branch)
    {
        if (branch is null) return;
        RunGitAction(() => _repositoryService.CheckoutBranch(branch.Name));
    }

    private bool CanCreateBranch() => _repositoryService.IsOpen && !string.IsNullOrWhiteSpace(NewBranchName);

    [RelayCommand(CanExecute = nameof(CanCreateBranch))]
    private void CreateBranch()
    {
        RunGitAction(() =>
        {
            _repositoryService.CreateBranch(NewBranchName);
            NewBranchName = string.Empty;
        });
    }

    partial void OnNewBranchNameChanged(string value) => CreateBranchCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void Delete(BranchModel? branch)
    {
        if (branch is null) return;
        RunGitAction(() => _repositoryService.DeleteBranch(branch.Name));
    }

    [RelayCommand]
    private void Merge(BranchModel? branch)
    {
        if (branch is null) return;
        RunGitAction(() => _repositoryService.MergeBranchIntoCurrent(branch.Name));
    }

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
