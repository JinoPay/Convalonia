using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for persisting workspace state
/// </summary>
public interface IWorkspacePersistenceService
{
    /// <summary>
    /// Save workspace state to persistent storage
    /// </summary>
    Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load workspace state from persistent storage
    /// </summary>
    Task<Workspace?> LoadWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load all workspaces from persistent storage
    /// </summary>
    Task<IEnumerable<Workspace>> LoadAllWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete workspace state from persistent storage
    /// </summary>
    Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save last active workspace ID
    /// </summary>
    Task SaveLastActiveWorkspaceAsync(Guid? workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get last active workspace ID
    /// </summary>
    Task<Guid?> GetLastActiveWorkspaceAsync(CancellationToken cancellationToken = default);
}
