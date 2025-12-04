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
        _workspaceName = workspace.Name;
        _workspacePath = workspace.Path;
        _currentBranch = workspace.GitBranch;
        _status = workspace.Status;
    }

    partial void OnSelectedAgentChanged(Agent? value)
    {
        if (value != null && _workspace != null)
        {
            SelectedAgentChatViewModel = new ChatViewModel(value, _workspace.Path, _toastService);
        }
        else
        {
            SelectedAgentChatViewModel = null;
        }
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
}
