using System;
using System.Collections.Generic;

namespace Convalonia.Services;

/// <summary>
/// Service for allocating unique port ranges to workspaces
/// </summary>
public interface IPortAllocator
{
    /// <summary>
    /// Allocates a port range for a workspace (or returns existing allocation)
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    /// <returns>Base port number (workspace gets base + 0 to 9)</returns>
    int AllocatePort(Guid workspaceId);

    /// <summary>
    /// Gets the allocated port for a workspace
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    /// <returns>Base port number</returns>
    /// <exception cref="InvalidOperationException">If no port is allocated</exception>
    int GetPort(Guid workspaceId);

    /// <summary>
    /// Releases the port allocation for a workspace
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    void ReleasePort(Guid workspaceId);

    /// <summary>
    /// Gets all current port allocations
    /// </summary>
    /// <returns>Dictionary of workspace ID to base port</returns>
    IReadOnlyDictionary<Guid, int> GetAllAllocations();

    /// <summary>
    /// Gets the full port range (10 ports) for a workspace
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    /// <returns>Array of 10 port numbers</returns>
    int[] GetPortRange(Guid workspaceId);
}
