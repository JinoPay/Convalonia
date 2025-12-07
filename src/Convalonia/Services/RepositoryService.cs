using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for managing repositories within workspaces
/// </summary>
public class RepositoryService : IRepositoryService
{
    private readonly IGitService _gitHubService;

    public RepositoryService(IGitService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <summary>
    /// Adds a repository from a local git project
    /// </summary>
    public async Task<Repository?> AddLocalRepositoryAsync(Workspace workspace, string localPath)
    {
        if (!await _gitHubService.IsGitRepositoryAsync(localPath))
        {
            throw new InvalidOperationException("The specified path is not a git repository");
        }

        var repoRoot = await _gitHubService.GetRepositoryRootAsync(localPath);
        if (repoRoot == null)
        {
            throw new InvalidOperationException("Could not determine repository root");
        }

        // Generate repository name from directory name
        var repoName = new DirectoryInfo(repoRoot).Name;
        var workspaceRepoPath = Path.Combine(workspace.Path, repoName);

        // Copy repository to workspace
        var copySuccess = await _gitHubService.CopyRepositoryAsync(repoRoot, workspaceRepoPath);
        if (!copySuccess)
        {
            throw new InvalidOperationException("Failed to copy repository");
        }

        // Get current branch
        var currentBranch = await GetCurrentBranchAsync(workspaceRepoPath);

        // Get remote origin
        var remoteOrigin = await GetRemoteOriginAsync(workspaceRepoPath);

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = repoName,
            RootPath = repoRoot,
            WorkspacePath = workspaceRepoPath,
            CurrentBranch = currentBranch,
            BaseBranch = currentBranch,
            RemoteOrigin = remoteOrigin,
            SearchArchivedBranches = false,
            CreatedAt = DateTime.Now,
            HasChanges = false
        };

        workspace.Repositories.Add(repository);
        return repository;
    }

    /// <summary>
    /// Adds a repository by cloning from URL
    /// </summary>
    public async Task<Repository?> AddRepositoryFromUrlAsync(Workspace workspace, string gitUrl, string? branchName = null)
    {
        // Extract repo name from URL
        var repoName = ExtractRepoNameFromUrl(gitUrl);
        var workspaceRepoPath = Path.Combine(workspace.Path, repoName);

        // Clone repository
        var cloneSuccess = await _gitHubService.CloneRepositoryAsync(gitUrl, workspaceRepoPath);
        if (!cloneSuccess)
        {
            throw new InvalidOperationException("Failed to clone repository");
        }

        // Checkout specific branch if provided
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            await CheckoutBranchAsync(workspaceRepoPath, branchName);
        }

        // Get current branch
        var currentBranch = await GetCurrentBranchAsync(workspaceRepoPath);

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = repoName,
            RootPath = null, // No local root for cloned repos
            WorkspacePath = workspaceRepoPath,
            CurrentBranch = currentBranch,
            BaseBranch = branchName ?? currentBranch,
            RemoteOrigin = gitUrl,
            SearchArchivedBranches = false,
            CreatedAt = DateTime.Now,
            HasChanges = false
        };

        workspace.Repositories.Add(repository);
        return repository;
    }

    /// <summary>
    /// Creates a new branch for the repository
    /// </summary>
    public async Task<bool> CreateBranchAsync(Repository repository, string branchName, string? baseBranch = null)
    {
        var baseBranchToUse = baseBranch ?? repository.BaseBranch ?? "main";
        var success = await _gitHubService.CreateBranchAsync(repository.WorkspacePath, branchName, baseBranchToUse);

        if (success)
        {
            repository.CurrentBranch = branchName;
        }

        return success;
    }

    /// <summary>
    /// Switches to a different branch
    /// </summary>
    public async Task<bool> CheckoutBranchAsync(Repository repository, string branchName)
    {
        var success = await CheckoutBranchAsync(repository.WorkspacePath, branchName);

        if (success)
        {
            repository.CurrentBranch = branchName;
        }

        return success;
    }

    /// <summary>
    /// Gets all branches for a repository
    /// </summary>
    public async Task<string[]> GetBranchesAsync(Repository repository, bool includeArchived = false)
    {
        return await GetBranchesAsync(repository.WorkspacePath, includeArchived);
    }

    /// <summary>
    /// Updates repository status (checks for uncommitted changes)
    /// </summary>
    public async Task UpdateRepositoryStatusAsync(Repository repository)
    {
        repository.HasChanges = await HasUncommittedChangesAsync(repository.WorkspacePath);
        repository.LastCommitHash = await GetLastCommitHashAsync(repository.WorkspacePath);
    }

    /// <summary>
    /// Removes a repository from workspace
    /// </summary>
    public async Task RemoveRepositoryAsync(Workspace workspace, Guid repositoryId)
    {
        var repository = workspace.Repositories.FirstOrDefault(r => r.Id == repositoryId);
        if (repository == null)
            return;

        // Delete directory
        if (Directory.Exists(repository.WorkspacePath))
        {
            Directory.Delete(repository.WorkspacePath, recursive: true);
        }

        workspace.Repositories.Remove(repository);

        await Task.CompletedTask;
    }

    // Helper methods
    private async Task<string?> GetCurrentBranchAsync(string repoPath)
    {
        return await _gitHubService.GetCurrentBranchAsync(repoPath);
    }

    private async Task<string?> GetRemoteOriginAsync(string repoPath)
    {
        return await _gitHubService.GetRemoteOriginAsync(repoPath);
    }

    private async Task<bool> CheckoutBranchAsync(string repoPath, string branchName)
    {
        return await _gitHubService.CheckoutBranchAsync(repoPath, branchName);
    }

    private async Task<string[]> GetBranchesAsync(string repoPath, bool includeArchived)
    {
        return await _gitHubService.GetBranchesAsync(repoPath, includeArchived);
    }

    private async Task<bool> HasUncommittedChangesAsync(string repoPath)
    {
        return await _gitHubService.HasUncommittedChangesAsync(repoPath);
    }

    private async Task<string?> GetLastCommitHashAsync(string repoPath)
    {
        return await _gitHubService.GetLastCommitHashAsync(repoPath);
    }

    private static string ExtractRepoNameFromUrl(string gitUrl)
    {
        // Extract repo name from URLs like:
        // https://github.com/user/repo.git -> repo
        // https://github.com/user/repo -> repo
        var uri = new Uri(gitUrl);
        var lastSegment = uri.Segments.LastOrDefault()?.TrimEnd('/') ?? "repository";
        return lastSegment.Replace(".git", string.Empty);
    }
}
