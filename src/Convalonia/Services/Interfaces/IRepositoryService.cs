using System;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Interface for repository management within workspaces
/// </summary>
public interface IRepositoryService
{
    Task<Repository?> AddLocalRepositoryAsync(Workspace workspace, string localPath);
    Task<Repository?> AddRepositoryFromUrlAsync(Workspace workspace, string gitUrl, string? branchName = null);
    Task<bool> CreateBranchAsync(Repository repository, string branchName, string? baseBranch = null);
    Task<bool> CheckoutBranchAsync(Repository repository, string branchName);
    Task<string[]> GetBranchesAsync(Repository repository, bool includeArchived = false);
    Task UpdateRepositoryStatusAsync(Repository repository);
    Task RemoveRepositoryAsync(Workspace workspace, Guid repositoryId);
}
