using Avalonia.Controls;
using Jinobald.Core.Services.Dialog;
using Jinobald.Core.Services.Toast;

namespace Convalonia;

public partial class MainWindow : Window
{
    public MainWindow(IDialogService dialogService, IToastService toastService)
    {
        InitializeComponent();

        // Register dialog and toast hosts
        dialogService.RegisterHost(DialogHost);
        toastService.RegisterHost(ToastHost);
    }
}