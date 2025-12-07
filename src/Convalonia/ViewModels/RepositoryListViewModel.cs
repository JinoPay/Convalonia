using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Views;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Toast;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for managing the list of source repositories
/// </summary>
public partial class RepositoryListViewModel : ViewModelBase
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;

    [ObservableProperty]
    private ObservableCollection<SourceRepository> _repositories;

    [ObservableProperty]
    private SourceRepository? _selectedRepository;

    public RepositoryListViewModel(
        RepositoryManagementService repositoryManagementService,
        IToastService toastService,
        IRegionManager regionManager)
    {
        _repositoryManagementService = repositoryManagementService;
        _toastService = toastService;
        _regionManager = regionManager;
        _repositories = _repositoryManagementService.Repositories;
    }

    /// <summary>
    /// Navigates to setup screen to add a new repository
    /// </summary>
    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        await _regionManager.NavigateAsync<SetupView>("MainContentRegion");
    }

    /// <summary>
    /// Removes a repository
    /// </summary>
    [RelayCommand]
    private async Task RemoveRepositoryAsync(SourceRepository repository)
    {
        if (repository == null) return;

        try
        {
            await _repositoryManagementService.RemoveRepositoryAsync(repository.Id);
            _toastService.ShowSuccess($"Repository '{repository.Name}' removed");

            if (SelectedRepository?.Id == repository.Id)
            {
                SelectedRepository = null;
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to remove repository: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects a repository and shows its workspaces
    /// </summary>
    [RelayCommand]
    private async Task SelectRepositoryAsync(SourceRepository repository)
    {
        SelectedRepository = repository;
        await _repositoryManagementService.UpdateLastAccessedAsync(repository.Id);

        // Navigate to repository detail view (shows workspaces)
        await _regionManager.NavigateAsync<RepositoryDetailView>("MainContentRegion", repository.Id);
    }

    /// <summary>
    /// Creates a new workspace for a repository
    /// </summary>
    [RelayCommand]
    private async Task CreateWorkspaceAsync(SourceRepository repository)
    {
        if (repository == null) return;

        try
        {
            var workspace = await _repositoryManagementService.CreateWorkspaceAsync(repository);
            _toastService.ShowSuccess($"Workspace '{workspace.Name}' created for repository '{repository.Name}'");

            // Navigate to the repository detail view to show the new workspace
            await _regionManager.NavigateAsync<RepositoryDetailView>("MainContentRegion", repository.Id);
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to create workspace: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RefreshRepositories()
    {
        _toastService.ShowInfo("Repositories refreshed");
    }

    /// <summary>
    /// Opens a workspace directly from the repository list
    /// </summary>
    [RelayCommand]
    private async Task OpenWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null) return;

        try
        {
            // Navigate to WorkspaceView with the selected workspace
            await _regionManager.NavigateAsync<WorkspaceView>("MainContentRegion", workspace.Id);
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to open workspace: {ex.Message}");
        }
    }
}
