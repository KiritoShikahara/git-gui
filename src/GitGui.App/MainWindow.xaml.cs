using System.Windows;
using GitGui.App.Native;
using GitGui.App.ViewModels;
using GitGui.App.Views;
using Microsoft.Win32;

namespace GitGui.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.RequestFolderPick += OnRequestFolderPick;
            vm.RequestCloneDialog += OnRequestCloneDialog;
        }
    }

    private string? OnRequestFolderPick()
    {
        var dialog = new OpenFolderDialog { Title = "Gitリポジトリのフォルダーを選択" };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private (string Url, string Destination)? OnRequestCloneDialog()
    {
        var dialog = new CloneDialog { Owner = this };
        return dialog.ShowDialog() == true ? (dialog.Url, dialog.Destination) : null;
    }
}
