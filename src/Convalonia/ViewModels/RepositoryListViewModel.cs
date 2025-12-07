using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Views;
using Convalonia.Services.Navigation;
using Convalonia.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for managing the list of source repositories
/// </summary>
public partial class RepositoryListViewModel : ReactiveObject
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;

    [Reactive]
    private ObservableCollection<SourceRepository> _repositories;

    [Reactive]
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
    [ReactiveCommand]
    private async Task AddRepositoryAsync()
    {
        await _regionManager.NavigateAsync<SetupView>("MainContentRegion");
    }

    /// <summary>
    /// Removes a repository
    /// </summary>
    [ReactiveCommand]
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
    [ReactiveCommand]
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
    [ReactiveCommand]
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

    [ReactiveCommand]
    private void RefreshRepositories()
    {
        _toastService.ShowInfo("Repositories refreshed");
    }

    /// <summary>
    /// Opens a workspace directly from the repository list
    /// </summary>
    [ReactiveCommand]
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
