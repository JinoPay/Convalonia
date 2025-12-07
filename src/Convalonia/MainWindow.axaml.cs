using Avalonia.Controls;
using Convalonia.Services;
using Convalonia.Views;
using Jinobald.Core.Services.Dialog;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Toast;
using Serilog;

namespace Convalonia;

public partial class MainWindow : Window
{
    private readonly IRegionManager _regionManager;
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly ILogger _logger = Log.ForContext<MainWindow>();

    public MainWindow(
        IDialogService dialogService,
        IToastService toastService,
        IRegionManager regionManager,
        RepositoryManagementService repositoryManagementService)
    {
        _regionManager = regionManager;
        _repositoryManagementService = repositoryManagementService;

        InitializeComponent();

        // Register dialog and toast hosts
        dialogService.RegisterHost(DialogHost);
        toastService.RegisterHost(ToastHost);

        // Navigate to appropriate initial screen after window is fully loaded
        // Use Loaded event instead of Opened to ensure Region is ready
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var hasRepositories = _repositoryManagementService.Repositories.Count > 0;

            _logger.Information("MainWindow opened. Repository count: {Count}", _repositoryManagementService.Repositories.Count);

            if (hasRepositories)
            {
                // Use the new unified main view
                _logger.Information("Navigating to UnifiedMainView");
                await _regionManager.NavigateAsync<UnifiedMainView>("MainContentRegion");
            }
            else
            {
                // Show setup if no repositories exist
                _logger.Information("No repositories found. Navigating to SetupView");
                await _regionManager.NavigateAsync<SetupView>("MainContentRegion");
            }
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Error during initial navigation");
        }
    }
}