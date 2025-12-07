using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for executing conductor.json scripts
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Executes the setup script for a workspace
    /// </summary>
    /// <param name="workspace">Workspace to run setup script for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteSetupScriptAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the run script for a workspace
    /// </summary>
    /// <param name="workspace">Workspace to run script for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Running process (or null if no script)</returns>
    Task<Process?> ExecuteRunScriptAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the archive script for a workspace
    /// </summary>
    /// <param name="workspace">Workspace to run archive script for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteArchiveScriptAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running script
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    void StopRunScript(Guid workspaceId);

    /// <summary>
    /// Checks if a run script is currently running for a workspace
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    /// <returns>True if a script is running</returns>
    bool IsRunScriptRunning(Guid workspaceId);

    /// <summary>
    /// Gets the currently running process for a workspace
    /// </summary>
    /// <param name="workspaceId">Workspace ID</param>
    /// <returns>Process or null if not running</returns>
    Process? GetRunningProcess(Guid workspaceId);
}
