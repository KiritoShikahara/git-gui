using System.Windows;
using Microsoft.Win32;

namespace GitGui.App.Views;

public partial class CloneDialog : Window
{
    public string Url => UrlTextBox.Text.Trim();
    public string Destination => DestinationTextBox.Text.Trim();

    public CloneDialog()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "保存先フォルダーを選択" };
        if (dialog.ShowDialog(this) == true)
        {
            DestinationTextBox.Text = dialog.FolderName;
        }
    }

    private void CloneButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(Destination))
        {
            MessageBox.Show(this, "URLと保存先フォルダーを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
