using System;
using System.Threading.Tasks;

namespace Convalonia.Services;

/// <summary>
/// Interface for Git operations (clone, branch, PR, etc.)
/// </summary>
public interface IGitService
{
    Task<bool> ValidateGitUrlAsync(string repoUrl);
    Task<bool> CloneRepositoryAsync(string repoUrl, string targetPath);
    Task<bool> CreateBranchAsync(string workspacePath, string branchName, string? baseBranch = null);
    Task<string?> GetCurrentBranchAsync(string workspacePath);
    Task<bool> CommitChangesAsync(string workspacePath, string message);
    Task<bool> PushChangesAsync(string workspacePath, string branchName);
    Task<bool> IsGitRepositoryAsync(string path);
    Task<bool> InitRepositoryAsync(string workspacePath);
    Task<string?> GetRepositoryRootAsync(string path);
    Task<bool> CopyRepositoryAsync(string sourceRepoPath, string targetPath);
    Task<bool> CheckoutBranchAsync(string workspacePath, string branchName);
    Task<string[]> GetBranchesAsync(string workspacePath, bool includeArchived = false);
    Task<string?> GetRemoteOriginAsync(string workspacePath);
    Task<bool> HasUncommittedChangesAsync(string workspacePath);
    Task<string?> GetLastCommitHashAsync(string workspacePath);
}
