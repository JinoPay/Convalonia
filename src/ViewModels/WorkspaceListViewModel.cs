using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Models;
using Convalonia.Services;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Toast;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for managing the list of workspaces
/// </summary>
public partial class WorkspaceListViewModel : ViewModelBase
{
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private ObservableCollection<Workspace> _workspaces;

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private string _newWorkspaceName = string.Empty;

    [ObservableProperty]
    private string _gitRepositoryUrl = string.Empty;

    [ObservableProperty]
    private bool _isCreatingWorkspace = false;

    public WorkspaceListViewModel(
        WorkspaceService workspaceService,
        IToastService toastService)
    {
        _workspaceService = workspaceService;
        _toastService = toastService;
        _workspaces = _workspaceService.Workspaces;
    }

    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        IsCreatingWorkspace = true;

        try
        {
            // Pass name to service (can be empty - service will generate random name)
            var workspaceName = string.IsNullOrWhiteSpace(NewWorkspaceName) ? null : NewWorkspaceName;

            var workspace = await _workspaceService.CreateWorkspaceAsync(
                workspaceName,
                string.IsNullOrWhiteSpace(GitRepositoryUrl) ? null : GitRepositoryUrl
            );

            _toastService.ShowSuccess($"Workspace '{workspace.Name}' created successfully!");

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

    [RelayCommand]
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

    [RelayCommand]
    private async Task SelectWorkspaceAsync(Workspace workspace)
    {
        SelectedWorkspace = workspace;
        _workspaceService.UpdateLastAccessed(workspace.Id);

        // TODO: Implement navigation to WorkspaceView
        // For now, just show a toast
        _toastService.ShowInfo($"Selected workspace: {workspace.Name}");

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshWorkspaces()
    {
        _toastService.ShowInfo("Workspaces refreshed");
    }
}
