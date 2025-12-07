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
/// Unified main view model that combines repository list, workspace, and file management
/// </summary>
public partial class UnifiedMainViewModel : ViewModelBase
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly WorkspaceService _workspaceService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;
    private readonly IClaudeCodeServiceFactory _claudeCodeServiceFactory;
    private readonly IScriptExecutor _scriptExecutor;

    [ObservableProperty]
    private ObservableCollection<SourceRepository> _repositories;

    [ObservableProperty]
    private SourceRepository? _selectedRepository;

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private Agent? _selectedAgent;

    [ObservableProperty]
    private ChatViewModel? _selectedAgentChatViewModel;

    [ObservableProperty]
    private bool _isTerminalVisible;

    [ObservableProperty]
    private bool _isRunScriptRunning;

    /// <summary>
    /// Available AI models for agent selection
    /// </summary>
    public ObservableCollection<string> AvailableModels { get; } = new()
    {
        "claude-sonnet-4-5-20250929",
        "claude-opus-4-20250514",
        "claude-sonnet-3-5-20241022",
        "claude-haiku-3-5-20241022"
    };

    public UnifiedMainViewModel(
        RepositoryManagementService repositoryManagementService,
        WorkspaceService workspaceService,
        IToastService toastService,
        IRegionManager regionManager,
        IClaudeCodeServiceFactory claudeCodeServiceFactory,
        IScriptExecutor scriptExecutor)
    {
        _repositoryManagementService = repositoryManagementService;
        _workspaceService = workspaceService;
        _toastService = toastService;
        _regionManager = regionManager;
        _claudeCodeServiceFactory = claudeCodeServiceFactory;
        _scriptExecutor = scriptExecutor;
        _repositories = _repositoryManagementService.Repositories;
    }

    /// <summary>
    /// Adds a new repository
    /// </summary>
    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        // Navigate to setup view for adding repository
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
            _toastService.ShowSuccess($"레포지토리 '{repository.Name}' 제거됨");

            if (SelectedRepository?.Id == repository.Id)
            {
                SelectedRepository = null;
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"레포지토리 제거 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new workspace for a repository
    /// </summary>
    [RelayCommand]
    private async Task CreateWorkspaceForRepositoryAsync(SourceRepository repository)
    {
        if (repository == null) return;

        try
        {
            var workspace = await _repositoryManagementService.CreateWorkspaceAsync(repository);
            _toastService.ShowSuccess($"워크스페이스 '{workspace.Name}' 생성됨");

            // Automatically select the newly created workspace
            SelectedWorkspace = workspace;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"워크스페이스 생성 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects a workspace
    /// </summary>
    [RelayCommand]
    private void SelectWorkspace(Workspace workspace)
    {
        if (workspace == null) return;

        SelectedWorkspace = workspace;
        _workspaceService.UpdateLastAccessed(workspace.Id);

        // Select the first agent if available
        if (workspace.Agents.Count > 0)
        {
            SelectedAgent = workspace.Agents[0];
        }
        else
        {
            SelectedAgent = null;
        }
    }

    /// <summary>
    /// Deletes a workspace
    /// </summary>
    [RelayCommand]
    private async Task DeleteWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null) return;

        try
        {
            await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
            _toastService.ShowSuccess($"워크스페이스 '{workspace.Name}' 삭제됨");

            if (SelectedWorkspace?.Id == workspace.Id)
            {
                SelectedWorkspace = null;
                SelectedAgent = null;
                SelectedAgentChatViewModel = null;
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"워크스페이스 삭제 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new chat (agent) in the selected workspace
    /// </summary>
    [RelayCommand]
    private async Task CreateNewChatAsync()
    {
        if (SelectedWorkspace == null) return;

        try
        {
            var agent = new Agent
            {
                Id = Guid.NewGuid(),
                Name = $"채팅 {SelectedWorkspace.Agents.Count + 1}",
                WorkspaceId = SelectedWorkspace.Id,
                Status = AgentStatus.Idle,
                CreatedAt = DateTime.Now
            };

            SelectedWorkspace.Agents.Add(agent);
            _toastService.ShowSuccess($"새 채팅 '{agent.Name}' 생성됨");

            // Automatically select the new agent
            SelectedAgent = agent;
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"채팅 생성 실패: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Selects a chat (agent)
    /// </summary>
    [RelayCommand]
    private void SelectChat(Agent agent)
    {
        SelectedAgent = agent;
    }

    /// <summary>
    /// Deletes an agent
    /// </summary>
    [RelayCommand]
    private async Task DeleteAgentAsync(Agent agent)
    {
        if (agent == null || SelectedWorkspace == null) return;

        try
        {
            SelectedWorkspace.Agents.Remove(agent);
            _toastService.ShowSuccess($"채팅 '{agent.Name}' 삭제됨");

            if (SelectedAgent?.Id == agent.Id)
            {
                SelectedAgent = SelectedWorkspace.Agents.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"채팅 삭제 실패: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Opens the workspace folder
    /// </summary>
    [RelayCommand]
    private async Task OpenWorkspaceFolderAsync()
    {
        if (SelectedWorkspace == null) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SelectedWorkspace.Path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"폴더 열기 실패: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Opens settings
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _toastService.ShowInfo("설정 화면은 곧 구현됩니다");
    }

    /// <summary>
    /// Runs the workspace script (from conductor.json)
    /// </summary>
    [RelayCommand]
    private async Task RunWorkspaceAsync()
    {
        if (SelectedWorkspace == null) return;

        try
        {
            IsRunScriptRunning = true;
            _toastService.ShowInfo($"워크스페이스 '{SelectedWorkspace.Name}' 실행 중...");

            await _scriptExecutor.ExecuteRunScriptAsync(SelectedWorkspace);

            _toastService.ShowSuccess("실행 스크립트 시작됨");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"실행 실패: {ex.Message}");
            IsRunScriptRunning = false;
        }
    }

    /// <summary>
    /// Stops the running workspace script
    /// </summary>
    [RelayCommand]
    private void StopWorkspace()
    {
        if (SelectedWorkspace == null) return;

        try
        {
            _scriptExecutor.StopRunScript(SelectedWorkspace.Id);
            IsRunScriptRunning = false;
            _toastService.ShowInfo("실행 스크립트 중지됨");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"중지 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles terminal visibility
    /// </summary>
    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
    }

    /// <summary>
    /// Updates run script status when workspace changes
    /// </summary>
    partial void OnSelectedWorkspaceChanged(Workspace? value)
    {
        if (value != null)
        {
            IsRunScriptRunning = _scriptExecutor.IsRunScriptRunning(value.Id);
        }
        else
        {
            IsRunScriptRunning = false;
        }
    }

    /// <summary>
    /// Updates selected agent chat view model when agent changes
    /// </summary>
    partial void OnSelectedAgentChanged(Agent? value)
    {
        // Unsubscribe from previous chat view model
        if (SelectedAgentChatViewModel != null)
        {
            SelectedAgentChatViewModel.FirstMessageSent -= OnFirstMessageSent;
        }

        if (value != null && SelectedWorkspace != null)
        {
            var chatViewModel = new ChatViewModel(value, SelectedWorkspace.Path, _toastService, _claudeCodeServiceFactory);
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
        try
        {
            if (SelectedWorkspace == null) return;

            // Suggest a name based on the message
            var suggestedName = WorkspaceNameSuggestionService.SuggestName(firstMessage);

            if (suggestedName == null) return;

            // Check if we should rename (only if current name is random)
            if (!WorkspaceNameSuggestionService.ShouldRename(SelectedWorkspace.Name, suggestedName))
                return;

            // Attempt to rename
            var success = await _workspaceService.RenameWorkspaceAsync(SelectedWorkspace.Id, suggestedName);

            if (success)
            {
                _toastService.ShowInfo($"워크스페이스 이름이 '{suggestedName}'으로 변경됨");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnFirstMessageSent: {ex.Message}");
            _toastService.ShowError($"워크스페이스 이름 변경 실패: {ex.Message}");
        }
    }
}
