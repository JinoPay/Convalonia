using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for adding a repository to a workspace
/// </summary>
public partial class AddRepositoryViewModel : ReactiveObject
{
    private readonly Workspace _workspace;
    private readonly RepositoryService _repositoryService;
    private readonly IToastService _toastService;

    [Reactive]
    private string _selectedMethod = "OpenProject"; // OpenProject, CloneFromUrl, QuickStart

    [Reactive]
    private string _localProjectPath = string.Empty;

    [Reactive]
    private string _gitUrl = string.Empty;

    [Reactive]
    private string _branchName = string.Empty;

    [Reactive]
    private bool _searchArchivedBranches = false;

    [Reactive]
    private bool _isProcessing = false;

    public AddRepositoryViewModel(
        Workspace workspace,
        RepositoryService repositoryService,
        IToastService toastService)
    {
        _workspace = workspace;
        _repositoryService = repositoryService;
        _toastService = toastService;
    }

    [ReactiveCommand]
    private async Task BrowseProjectAsync()
    {
        // This will be implemented with Avalonia file picker
        // For now, users can manually enter the path
        await Task.CompletedTask;
    }

    [ReactiveCommand]
    private async Task AddRepositoryAsync()
    {
        IsProcessing = true;

        try
        {
            Repository? repository = null;

            switch (SelectedMethod)
            {
                case "OpenProject":
                    if (string.IsNullOrWhiteSpace(LocalProjectPath))
                    {
                        _toastService.ShowError("Please specify a local project path");
                        return;
                    }

                    if (!Directory.Exists(LocalProjectPath))
                    {
                        _toastService.ShowError("The specified path does not exist");
                        return;
                    }

                    repository = await _repositoryService.AddLocalRepositoryAsync(_workspace, LocalProjectPath);
                    break;

                case "CloneFromUrl":
                    if (string.IsNullOrWhiteSpace(GitUrl))
                    {
                        _toastService.ShowError("Please specify a git repository URL");
                        return;
                    }

                    repository = await _repositoryService.AddRepositoryFromUrlAsync(
                        _workspace,
                        GitUrl,
                        string.IsNullOrWhiteSpace(BranchName) ? null : BranchName);
                    break;

                case "QuickStart":
                    // Quick start creates an empty repository
                    _toastService.ShowInfo("Quick start not yet implemented");
                    return;

                default:
                    _toastService.ShowError("Unknown method selected");
                    return;
            }

            if (repository != null)
            {
                repository.SearchArchivedBranches = SearchArchivedBranches;
                _toastService.ShowSuccess($"Repository '{repository.Name}' added successfully!");

                // Reset form
                LocalProjectPath = string.Empty;
                GitUrl = string.Empty;
                BranchName = string.Empty;
                SearchArchivedBranches = false;
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to add repository: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [ReactiveCommand]
    private void SelectMethod(string method)
    {
        SelectedMethod = method;
    }
}
