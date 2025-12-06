using System;
using System.Collections.ObjectModel;
using System.Linq;
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
/// ViewModel for showing a repository's workspaces
/// </summary>
public partial class RepositoryDetailViewModel : ViewModelBase
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;

    [ObservableProperty]
    private SourceRepository? _repository;

    [ObservableProperty]
    private ObservableCollection<Workspace> _workspaces = new();

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private string _newWorkspaceName = string.Empty;

    [ObservableProperty]
    private bool _isCreatingWorkspace = false;

    public RepositoryDetailViewModel(
        RepositoryManagementService repositoryManagementService,
        WorkspaceService workspaceService,
        IToastService toastService,
        IRegionManager regionManager)
    {
        _repositoryManagementService = repositoryManagementService;
        _workspaceService = workspaceService;
        _toastService = toastService;
        _regionManager = regionManager;
    }

    public void Initialize(Guid repositoryId)
    {
        Repository = _repositoryManagementService.Repositories
            .FirstOrDefault(r => r.Id == repositoryId);

        if (Repository != null)
        {
            Workspaces = Repository.Workspaces;
        }
    }

    /// <summary>
    /// Goes back to repository list
    /// </summary>
    [RelayCommand]
    private async Task BackToRepositoryListAsync()
    {
        await _regionManager.NavigateAsync<RepositoryListView>("MainContentRegion");
    }

    /// <summary>
    /// Creates a new workspace for this repository
    /// </summary>
    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        if (Repository == null) return;

        IsCreatingWorkspace = true;

        try
        {
            var workspaceName = string.IsNullOrWhiteSpace(NewWorkspaceName) ? null : NewWorkspaceName;
            var workspace = await _repositoryManagementService.CreateWorkspaceAsync(Repository, workspaceName);

            _toastService.ShowSuccess($"Workspace '{workspace.Name}' created!");

            NewWorkspaceName = string.Empty;
            SelectedWorkspace = workspace;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to create workspace: {ex.Message}");
        }
        finally
        {
            IsCreatingWorkspace = false;
        }
    }

    /// <summary>
    /// Deletes a workspace
    /// </summary>
    [RelayCommand]
    private async Task DeleteWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null || Repository == null) return;

        try
        {
            await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
            Repository.Workspaces.Remove(workspace);
            _toastService.ShowSuccess($"Workspace '{workspace.Name}' deleted");

            if (SelectedWorkspace?.Id == workspace.Id)
            {
                SelectedWorkspace = null;
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to delete workspace: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects a workspace and navigates to it
    /// </summary>
    [RelayCommand]
    private async Task SelectWorkspaceAsync(Workspace workspace)
    {
        SelectedWorkspace = workspace;
        _workspaceService.UpdateLastAccessed(workspace.Id);

        // Navigate to WorkspaceView with the selected workspace
        await _regionManager.NavigateAsync<WorkspaceView>("MainContentRegion", workspace.Id);
    }
}
