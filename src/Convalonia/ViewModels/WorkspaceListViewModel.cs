using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services;
using Jinobald.Core.Services.Toast;
using Jinobald.Core.Services.Regions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for managing the list of workspaces
/// </summary>
public partial class WorkspaceListViewModel : ReactiveObject
{
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;

    [Reactive]
    private ObservableCollection<Workspace> _workspaces;

    [Reactive]
    private Workspace? _selectedWorkspace;

    [Reactive]
    private string _newWorkspaceName = string.Empty;

    [Reactive]
    private string _gitRepositoryUrl = string.Empty;

    [Reactive]
    private bool _isCreatingWorkspace = false;

    public WorkspaceListViewModel(
        WorkspaceService workspaceService,
        IToastService toastService,
        IRegionManager regionManager)
    {
        _workspaceService = workspaceService;
        _toastService = toastService;
        _regionManager = regionManager;
        _workspaces = _workspaceService.Workspaces;
    }

    [ReactiveCommand]
    private async Task CreateWorkspaceAsync()
    {
        IsCreatingWorkspace = true;

        try
        {
            // Pass name to service (can be empty - service will generate random name)
            var workspaceName = string.IsNullOrWhiteSpace(NewWorkspaceName) ? null : NewWorkspaceName;

            // Auto-detect current directory as source repo
            var currentDirectory = Directory.GetCurrentDirectory();

            var workspace = await _workspaceService.CreateWorkspaceAsync(
                workspaceName,
                string.IsNullOrWhiteSpace(GitRepositoryUrl) ? null : GitRepositoryUrl,
                currentDirectory  // Always pass current directory, service will check if it's a git repo
            );

            // Show success message with git branch info if available
            var message = string.IsNullOrWhiteSpace(workspace.GitBranch)
                ? $"Workspace '{workspace.Name}' created successfully!"
                : $"Workspace '{workspace.Name}' created with branch '{workspace.GitBranch}'!";

            _toastService.ShowSuccess(message);

            // Reset form
            NewWorkspaceName = string.Empty;
            GitRepositoryUrl = string.Empty;

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

    [ReactiveCommand]
    private async Task DeleteWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null)
            return;

        try
        {
            await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
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

    [ReactiveCommand]
    private async Task SelectWorkspaceAsync(Workspace workspace)
    {
        SelectedWorkspace = workspace;
        _workspaceService.UpdateLastAccessed(workspace.Id);

        // Navigate to WorkspaceView with the selected workspace
        await _regionManager.NavigateAsync<Views.WorkspaceView>("MainContentRegion", workspace.Id);
    }

    [ReactiveCommand]
    private void RefreshWorkspaces()
    {
        _toastService.ShowInfo("Workspaces refreshed");
    }
}
