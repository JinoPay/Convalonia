using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Models;
using Convalonia.Services;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Toast;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for a single workspace with multiple agents
/// </summary>
public partial class WorkspaceViewModel : ViewModelBase
{
    private Workspace? _workspace;
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private ObservableCollection<Agent> _agents;

    [ObservableProperty]
    private Agent? _selectedAgent;

    [ObservableProperty]
    private ObservableCollection<Repository> _repositories;

    [ObservableProperty]
    private Repository? _selectedRepository;

    [ObservableProperty]
    private string _workspaceName;

    [ObservableProperty]
    private string _workspacePath;

    [ObservableProperty]
    private string? _currentBranch;

    [ObservableProperty]
    private WorkspaceStatus _status;

    [ObservableProperty]
    private ChatViewModel? _selectedAgentChatViewModel;

    public WorkspaceViewModel(
        Workspace workspace,
        WorkspaceService workspaceService,
        IToastService toastService)
    {
        _workspace = workspace;
        _workspaceService = workspaceService;
        _toastService = toastService;

        _agents = workspace.Agents;
        _repositories = workspace.Repositories;
        _workspaceName = workspace.Name;
        _workspacePath = workspace.Path;
        _currentBranch = workspace.GitBranch;
        _status = workspace.Status;
    }

    partial void OnSelectedAgentChanged(Agent? value)
    {
        // Unsubscribe from previous chat view model
        if (SelectedAgentChatViewModel != null)
        {
            SelectedAgentChatViewModel.FirstMessageSent -= OnFirstMessageSent;
        }

        if (value != null && _workspace != null)
        {
            var chatViewModel = new ChatViewModel(value, _workspace.Path, _toastService);
            chatViewModel.FirstMessageSent += OnFirstMessageSent;
            SelectedAgentChatViewModel = chatViewModel;
        }
        else
        {
            SelectedAgentChatViewModel = null;
        }
    }

    private async void OnFirstMessageSent(object? sender, string firstMessage)
    {
        await TryAutoRenameWorkspaceAsync(firstMessage);
    }

    [RelayCommand]
    private async Task CreateAgentAsync()
    {
        if (_workspace == null)
            return;

        try
        {
            var agent = new Agent
            {
                Id = Guid.NewGuid(),
                Name = $"Agent {_agents.Count + 1}",
                WorkspaceId = _workspace.Id,
                Status = AgentStatus.Idle,
                CreatedAt = DateTime.Now
            };

            _agents.Add(agent);
            _toastService.ShowSuccess($"Agent '{agent.Name}' created");

            SelectedAgent = agent;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to create agent: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteAgentAsync(Agent agent)
    {
        if (agent == null)
            return;

        try
        {
            _agents.Remove(agent);
            _toastService.ShowSuccess($"Agent '{agent.Name}' deleted");

            if (SelectedAgent?.Id == agent.Id)
            {
                SelectedAgent = _agents.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to delete agent: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectAgent(Agent agent)
    {
        SelectedAgent = agent;
    }

    [RelayCommand]
    private async Task StopAllAgentsAsync()
    {
        try
        {
            foreach (var agent in _agents)
            {
                if (agent.Status == AgentStatus.Thinking || agent.Status == AgentStatus.UsingTool)
                {
                    agent.Status = AgentStatus.Idle;
                }
            }

            _toastService.ShowInfo("All agents stopped");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to stop agents: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenWorkspaceFolderAsync()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _workspacePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to open folder: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        if (_workspace == null)
            return;

        try
        {
            // This will open the AddRepositoryDialog
            // For now, just show a toast
            _toastService.ShowInfo("Add repository dialog will be shown here");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to add repository: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RemoveRepositoryAsync(Repository repository)
    {
        if (_workspace == null || repository == null)
            return;

        try
        {
            await _workspaceService.RepositoryService.RemoveRepositoryAsync(_workspace, repository.Id);
            _toastService.ShowSuccess($"Repository '{repository.Name}' removed");

            if (SelectedRepository?.Id == repository.Id)
            {
                SelectedRepository = _repositories.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to remove repository: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshRepositoryStatusAsync(Repository repository)
    {
        if (repository == null)
            return;

        try
        {
            await _workspaceService.RepositoryService.UpdateRepositoryStatusAsync(repository);
            _toastService.ShowInfo($"Repository '{repository.Name}' status updated");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to refresh repository: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenRepositoryFolderAsync(Repository repository)
    {
        if (repository == null)
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = repository.WorkspacePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to open folder: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Attempts to auto-rename workspace based on first user message
    /// </summary>
    public async Task TryAutoRenameWorkspaceAsync(string firstUserMessage)
    {
        if (_workspace == null)
            return;

        // Suggest a name based on the message
        var suggestedName = WorkspaceNameSuggestionService.SuggestName(firstUserMessage);

        if (suggestedName == null)
            return;

        // Check if we should rename (only if current name is random)
        if (!WorkspaceNameSuggestionService.ShouldRename(_workspace.Name, suggestedName))
            return;

        // Attempt to rename
        var success = await _workspaceService.RenameWorkspaceAsync(_workspace.Id, suggestedName);

        if (success)
        {
            WorkspaceName = suggestedName;
            _toastService.ShowInfo($"Workspace renamed to '{suggestedName}'");
        }
    }
}
