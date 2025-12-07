using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Services;
using Convalonia.Views;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Dialog;
using Jinobald.Core.Services.Regions;
using Jinobald.Core.Services.Toast;
using Serilog;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for initial setup screen when no repositories exist
/// </summary>
public partial class SetupViewModel : ViewModelBase
{
    private readonly RepositoryManagementService _repositoryManagementService;
    private readonly GitHubService _gitHubService;
    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly IRegionManager _regionManager;
    private readonly ILogger _logger = Log.ForContext<SetupViewModel>();

    [ObservableProperty]
    private bool _isClaudeCodeInstalled;

    [ObservableProperty]
    private bool _isCheckingClaudeCode;

    [ObservableProperty]
    private string _gitUrl = string.Empty;

    [ObservableProperty]
    private bool _isCloningRepository;

    public SetupViewModel(
        RepositoryManagementService repositoryManagementService,
        GitHubService gitHubService,
        IDialogService dialogService,
        IToastService toastService,
        IRegionManager regionManager)
    {
        _repositoryManagementService = repositoryManagementService;
        _gitHubService = gitHubService;
        _dialogService = dialogService;
        _toastService = toastService;
        _regionManager = regionManager;

        // Check Claude Code on initialization
        _ = CheckClaudeCodeInstallationAsync();
    }

    /// <summary>
    /// Checks if Claude Code is installed
    /// </summary>
    private async Task CheckClaudeCodeInstallationAsync()
    {
        IsCheckingClaudeCode = true;
        try
        {
            IsClaudeCodeInstalled = await ClaudeCodeService.IsClaudeCodeInstalledAsync();
        }
        finally
        {
            IsCheckingClaudeCode = false;
        }
    }

    /// <summary>
    /// Opens a folder picker to select a local git repository
    /// </summary>
    [RelayCommand]
    private async Task SelectLocalRepositoryAsync()
    {
        if (!IsClaudeCodeInstalled)
        {
            await ShowErrorAsync("Claude Code가 설치되어 있지 않습니다",
                "계속하려면 먼저 Claude Code CLI를 설치해주세요.");
            return;
        }

        var folder = await PickFolderAsync("로컬 Git 레포지토리 선택");
        if (folder == null) return;

        var folderPath = folder.Path.LocalPath;

        // Validate it's a git repository
        if (!await _gitHubService.IsGitRepositoryAsync(folderPath))
        {
            await ShowErrorAsync("Git 레포지토리가 아닙니다",
                "선택한 폴더는 Git 레포지토리가 아닙니다.\nGit 레포지토리가 있는 폴더를 선택해주세요.");
            return;
        }

        // Add repository
        try
        {
            var repository = await _repositoryManagementService.AddLocalRepositoryAsync(folderPath);

            if (repository != null)
            {
                _toastService.ShowSuccess($"Repository '{repository.Name}' added successfully!");
                // Navigate to unified main view
                await _regionManager.NavigateAsync<UnifiedMainView>("MainContentRegion");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("레포지토리 추가 실패",
                $"레포지토리를 추가하는 중 오류가 발생했습니다:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Clones a repository from a git URL
    /// </summary>
    [RelayCommand]
    private async Task CloneFromUrlAsync()
    {
        if (!IsClaudeCodeInstalled)
        {
            await ShowErrorAsync("Claude Code가 설치되어 있지 않습니다",
                "계속하려면 먼저 Claude Code CLI를 설치해주세요.");
            return;
        }

        if (string.IsNullOrWhiteSpace(GitUrl))
        {
            await ShowErrorAsync("URL을 입력해주세요",
                "Git 레포지토리 URL을 입력해주세요.");
            return;
        }

        IsCloningRepository = true;
        try
        {
            // Validate git URL
            if (!await _gitHubService.ValidateGitUrlAsync(GitUrl))
            {
                await ShowErrorAsync("잘못된 Git URL",
                    "입력한 URL에 접근할 수 없습니다.\nURL을 확인하고 다시 시도해주세요.");
                return;
            }

            // Add repository from git URL
            var repository = await _repositoryManagementService.AddRemoteRepositoryAsync(GitUrl);

            if (repository != null)
            {
                _toastService.ShowSuccess($"Repository '{repository.Name}' added successfully!");
                // Navigate to unified main view
                await _regionManager.NavigateAsync<UnifiedMainView>("MainContentRegion");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("레포지토리 클론 실패",
                $"레포지토리를 클론하는 중 오류가 발생했습니다:\n{ex.Message}");
        }
        finally
        {
            IsCloningRepository = false;
        }
    }

    /// <summary>
    /// Creates a new project with git initialization
    /// </summary>
    [RelayCommand]
    private async Task CreateNewProjectAsync()
    {
        if (!IsClaudeCodeInstalled)
        {
            await ShowErrorAsync("Claude Code가 설치되어 있지 않습니다",
                "계속하려면 먼저 Claude Code CLI를 설치해주세요.");
            return;
        }

        var folder = await PickFolderAsync("새 프로젝트 폴더 선택");
        if (folder == null) return;

        var folderPath = folder.Path.LocalPath;

        // Check if folder already has git
        if (await _gitHubService.IsGitRepositoryAsync(folderPath))
        {
            await ShowErrorAsync("이미 Git 레포지토리입니다",
                "선택한 폴더는 이미 Git 레포지토리입니다.\n'로컬 Git 레포지토리 열기'를 사용해주세요.");
            return;
        }

        // Check if folder is empty
        var isEmpty = !System.IO.Directory.EnumerateFileSystemEntries(folderPath).Any();
        var folderTypeMessage = isEmpty
            ? "이 빈 폴더에 Git을 초기화합니다."
            : "이 폴더에 Git을 초기화합니다.\n(기존 파일은 유지됩니다)";

        // Ask user to confirm git initialization
        var confirmed = await ConfirmAsync("Git 초기화",
            $"{folderTypeMessage}\n\n계속하시겠습니까?");

        if (!confirmed) return;

        try
        {
            // Create new repository (initializes git)
            var repository = await _repositoryManagementService.CreateNewRepositoryAsync(folderPath);

            if (repository != null)
            {
                _toastService.ShowSuccess($"Repository '{repository.Name}' created successfully!");
                // Navigate to unified main view
                await _regionManager.NavigateAsync<UnifiedMainView>("MainContentRegion");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("프로젝트 생성 실패",
                $"프로젝트를 생성하는 중 오류가 발생했습니다:\n{ex.Message}");
        }
    }

    private async Task<IStorageFolder?> PickFolderAsync(string title)
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.StorageProvider
            : null;

        if (topLevel == null) return null;

        var folders = await topLevel.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0] : null;
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        _toastService.ShowError($"{title}: {message}");
        await Task.CompletedTask;
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        // TODO: Implement proper confirmation dialog with custom ConfirmationDialog view
        // For now, show toast and auto-confirm for non-destructive operations
        _toastService.ShowInfo($"{title}: {message}");
        _logger.Information("User confirmation required: {Title} - {Message}", title, message);
        await Task.CompletedTask;
        return true;
    }
}
