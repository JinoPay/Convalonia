using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Views;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for showing a repository's workspaces
/// </summary>
public partial class RepositoryDetailViewModel : ReactiveObject
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;

    [Reactive]
    private SourceRepository? _repository;

    [Reactive]
    private ObservableCollection<Workspace> _workspaces = new();

    [Reactive]
    private Workspace? _selectedWorkspace;

    [Reactive]
    private string _newWorkspaceName = string.Empty;

    [Reactive]
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
    [ReactiveCommand]
    private async Task BackToRepositoryListAsync()
    {
        await _regionManager.NavigateAsync<RepositoryListView>("MainContentRegion");
    }

    /// <summary>
    /// Creates a new workspace for this repository
    /// </summary>
    [ReactiveCommand]
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
    [ReactiveCommand]
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
    [ReactiveCommand]
    private async Task SelectWorkspaceAsync(Workspace workspace)
    {
        SelectedWorkspace = workspace;
        _workspaceService.UpdateLastAccessed(workspace.Id);

        // Navigate to WorkspaceView with the selected workspace
        await _regionManager.NavigateAsync<WorkspaceView>("MainContentRegion", workspace.Id);
    }
}
