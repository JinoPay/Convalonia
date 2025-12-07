using Avalonia.Controls;
using Convalonia.Services;
using Convalonia.Views;
using Jinobald.Core.Services.Dialog;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Toast;

namespace Convalonia;

public partial class MainWindow : Window
{
    public MainWindow(
        IDialogService dialogService,
        IToastService toastService,
        IRegionManager regionManager,
        RepositoryManagementService repositoryManagementService)
    {
        InitializeComponent();

        // Register dialog and toast hosts
        dialogService.RegisterHost(DialogHost);
        toastService.RegisterHost(ToastHost);

        // Navigate to appropriate initial screen
        Opened += async (s, e) =>
        {
            var hasRepositories = repositoryManagementService.Repositories.Count > 0;
            if (hasRepositories)
            {
                // Use the new unified main view
                await regionManager.NavigateAsync<UnifiedMainView>("MainContentRegion");
            }
            else
            {
                // Show setup if no repositories exist
                await regionManager.NavigateAsync<SetupView>("MainContentRegion");
            }
        };
    }
}