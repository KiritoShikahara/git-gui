using System.Collections.ObjectModel;
using System.Windows;
using GitGui.App.Native;
using GitGui.Core.Models;

namespace GitGui.App.Views;

public partial class IdentitiesDialog : Window
{
    public ObservableCollection<GitIdentityModel> Identities { get; }

    public IdentitiesDialog(IEnumerable<GitIdentityModel> identities)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);

        Identities = new ObservableCollection<GitIdentityModel>(identities);
        IdentityListBox.ItemsSource = Identities;
    }

    private void IdentityListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (IdentityListBox.SelectedItem is GitIdentityModel identity)
        {
            NameTextBox.Text = identity.Name;
            EmailTextBox.Text = identity.Email;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show(this, "名前とメールアドレスを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var identity = new GitIdentityModel { Name = name, Email = email };
        if (Identities.Contains(identity))
        {
            MessageBox.Show(this, "同じ名前とメールアドレスのアカウントが既に登録されています。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Identities.Add(identity);
        IdentityListBox.SelectedItem = identity;
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (IdentityListBox.SelectedItem is not GitIdentityModel selected) return;

        var name = NameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show(this, "名前とメールアドレスを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var index = Identities.IndexOf(selected);
        var updated = new GitIdentityModel { Name = name, Email = email };
        Identities[index] = updated;
        IdentityListBox.SelectedItem = updated;
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (IdentityListBox.SelectedItem is not GitIdentityModel selected) return;
        Identities.Remove(selected);
        NameTextBox.Clear();
        EmailTextBox.Clear();
    }
}
