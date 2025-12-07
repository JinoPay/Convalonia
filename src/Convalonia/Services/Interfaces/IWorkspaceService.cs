using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Interface for workspace management service
/// </summary>
public interface IWorkspaceService
{
    ObservableCollection<Workspace> Workspaces { get; }
    IRepositoryService RepositoryService { get; }

    Task<Workspace> CreateWorkspaceAsync(string? name = null, string? gitRemote = null, string? sourceRepoPath = null);
    Task DeleteWorkspaceAsync(Guid workspaceId);
    Workspace? GetWorkspace(Guid workspaceId);
    void UpdateLastAccessed(Guid workspaceId);
    Task<bool> RenameWorkspaceAsync(Guid workspaceId, string newName);
}
