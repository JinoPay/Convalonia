using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Convalonia.Views;

public partial class AddRepositoryDialog : Window
{
    public AddRepositoryDialog()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
