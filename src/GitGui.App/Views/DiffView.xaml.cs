using System.Windows.Controls;

namespace GitGui.App.Views;

/// <summary>Displays a single DiffResultModel (set as this control's DataContext).</summary>
public partial class DiffView : UserControl
{
    public DiffView()
    {
        InitializeComponent();
    }
}
