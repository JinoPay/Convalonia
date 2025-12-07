using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Convalonia.Views;

public partial class BranchSelectorDialog : Window
{
    public BranchSelectorDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
