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
using Serilog;

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
    private readonly ICheckpointService _checkpointService;
    private readonly IGitService _gitService;
    private readonly ILogger _logger = Log.ForContext<UnifiedMainViewModel>();

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
    private DiffViewerViewModel? _diffViewerViewModel;

    [ObservableProperty]
    private bool _isTerminalVisible;

    [ObservableProperty]
    private bool _isRunScriptRunning;

    [ObservableProperty]
    private string _selectedMainTab = "Chat";

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
        IScriptExecutor scriptExecutor,
        ICheckpointService checkpointService,
        IGitService gitService,
        DiffViewerViewModel diffViewerViewModel)
    {
        _repositoryManagementService = repositoryManagementService;
        _workspaceService = workspaceService;
        _toastService = toastService;
        _regionManager = regionManager;
        _claudeCodeServiceFactory = claudeCodeServiceFactory;
        _scriptExecutor = scriptExecutor;
        _checkpointService = checkpointService;
        _gitService = gitService;
        _diffViewerViewModel = diffViewerViewModel;
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
    /// Updates run script status and diff viewer when workspace changes
    /// </summary>
    partial void OnSelectedWorkspaceChanged(Workspace? value)
    {
        if (value != null)
        {
            IsRunScriptRunning = _scriptExecutor.IsRunScriptRunning(value.Id);

            // Update diff viewer with new workspace
            if (DiffViewerViewModel != null)
            {
                _ = DiffViewerViewModel.SetWorkspaceAsync(value);
            }
        }
        else
        {
            IsRunScriptRunning = false;
        }
    }

    /// <summary>
    /// Switches to the Files tab
    /// </summary>
    [RelayCommand]
    private void ShowDiffViewer()
    {
        SelectedMainTab = "Files";
        if (DiffViewerViewModel != null && SelectedWorkspace != null)
        {
            _ = DiffViewerViewModel.RefreshAsync();
        }
    }

    /// <summary>
    /// Creates a pull request for the current workspace
    /// </summary>
    [RelayCommand]
    private async Task CreatePullRequestAsync()
    {
        if (SelectedWorkspace == null)
        {
            _toastService.ShowWarning("워크스페이스를 선택하세요");
            return;
        }

        try
        {
            _toastService.ShowInfo("PR 생성 중...");

            // 1. Get current branch
            var currentBranch = await _gitService.GetCurrentBranchAsync(SelectedWorkspace.Path);
            if (string.IsNullOrEmpty(currentBranch))
            {
                _toastService.ShowError("현재 브랜치를 가져올 수 없습니다");
                return;
            }

            // 2. Check for uncommitted changes
            var hasUncommitted = await _gitService.HasUncommittedChangesAsync(SelectedWorkspace.Path);
            if (hasUncommitted)
            {
                _toastService.ShowWarning("커밋되지 않은 변경사항이 있습니다. 먼저 커밋하세요");
                return;
            }

            // 3. Push to remote
            _toastService.ShowInfo($"브랜치 '{currentBranch}' 푸시 중...");
            var pushed = await _gitService.PushBranchAsync(SelectedWorkspace.Path, currentBranch, setUpstream: true);
            if (!pushed)
            {
                _toastService.ShowError("브랜치 푸시 실패");
                return;
            }

            // 4. Generate PR title and body
            var prTitle = GeneratePRTitle(SelectedWorkspace, currentBranch);
            var prBody = await GeneratePRBodyAsync(SelectedWorkspace);

            // 5. Create PR using GitHub CLI
            _toastService.ShowInfo("GitHub PR 생성 중...");
            var prUrl = await _gitService.CreatePullRequestAsync(
                SelectedWorkspace.Path,
                prTitle,
                prBody,
                baseBranch: "main"
            );

            if (prUrl != null)
            {
                _toastService.ShowSuccess($"PR 생성 완료!");
                _logger.Information("Created PR for workspace {WorkspaceName}: {PrUrl}", SelectedWorkspace.Name, prUrl);

                // Open PR URL in browser
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = prUrl,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to open PR URL in browser");
                }
            }
            else
            {
                _toastService.ShowError("PR 생성 실패. GitHub CLI가 설치되어 있는지 확인하세요");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create pull request for workspace {WorkspacePath}", SelectedWorkspace?.Path);
            _toastService.ShowError($"PR 생성 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates PR title from workspace name and branch
    /// </summary>
    private string GeneratePRTitle(Workspace workspace, string branchName)
    {
        // Remove common prefixes like "feature/", "bugfix/", "JinoPay/"
        var cleanBranch = branchName;
        var prefixes = new[] { "feature/", "bugfix/", "hotfix/", "JinoPay/", "fix/", "feat/" };
        foreach (var prefix in prefixes)
        {
            if (cleanBranch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleanBranch = cleanBranch[prefix.Length..];
                break;
            }
        }

        // Convert kebab-case or snake_case to Title Case
        cleanBranch = cleanBranch
            .Replace("-", " ")
            .Replace("_", " ");

        // Capitalize first letter of each word
        var words = cleanBranch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        cleanBranch = string.Join(" ", words.Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w
        ));

        return cleanBranch;
    }

    /// <summary>
    /// Generates PR body with summary of changes
    /// </summary>
    private async Task<string> GeneratePRBodyAsync(Workspace workspace)
    {
        var body = new System.Text.StringBuilder();

        body.AppendLine("## Summary");
        body.AppendLine();

        // Get changed files
        var changedFiles = await _gitService.GetChangedFilesAsync(workspace.Path, includeUntracked: false);
        if (changedFiles.Length > 0)
        {
            body.AppendLine($"- {changedFiles.Length} file(s) changed");
        }

        // Get diff stats
        var diff = await _gitService.GetDiffAsync(workspace.Path, "main...HEAD");
        if (!string.IsNullOrEmpty(diff))
        {
            var lines = diff.Split('\n');
            var addedLines = lines.Count(l => l.StartsWith("+") && !l.StartsWith("+++"));
            var deletedLines = lines.Count(l => l.StartsWith("-") && !l.StartsWith("---"));
            body.AppendLine($"- +{addedLines} -{deletedLines} lines");
        }

        body.AppendLine();
        body.AppendLine("## Changes");
        body.AppendLine();

        if (changedFiles.Length > 0)
        {
            foreach (var file in changedFiles.Take(10))
            {
                body.AppendLine($"- `{file}`");
            }

            if (changedFiles.Length > 10)
            {
                body.AppendLine($"- ... and {changedFiles.Length - 10} more files");
            }
        }
        else
        {
            body.AppendLine("No changes detected.");
        }

        body.AppendLine();
        body.AppendLine("## Test Plan");
        body.AppendLine("- [ ] Manual testing completed");
        body.AppendLine("- [ ] Unit tests added/updated");
        body.AppendLine("- [ ] Integration tests passed");
        body.AppendLine();
        body.AppendLine("🤖 Generated with [Claude Code](https://claude.com/claude-code)");

        return body.ToString();
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
            var chatViewModel = new ChatViewModel(value, SelectedWorkspace, _toastService, _claudeCodeServiceFactory, _checkpointService);
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
