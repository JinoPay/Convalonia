using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for managing checkpoints (snapshots) of workspace state
/// </summary>
public interface ICheckpointService
{
    /// <summary>
    /// Creates a checkpoint for the current workspace state
    /// </summary>
    /// <param name="workspace">Workspace to checkpoint</param>
    /// <param name="agent">Agent to checkpoint</param>
    /// <param name="turnNumber">Turn number</param>
    /// <param name="userMessage">User message</param>
    /// <param name="assistantMessage">Assistant message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created checkpoint</returns>
    Task<Checkpoint> CreateCheckpointAsync(
        Workspace workspace,
        Agent agent,
        int turnNumber,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts workspace and agent to a previous checkpoint
    /// </summary>
    /// <param name="checkpoint">Checkpoint to revert to</param>
    /// <param name="workspace">Workspace to revert</param>
    /// <param name="agent">Agent to revert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevertToCheckpointAsync(
        Checkpoint checkpoint,
        Workspace workspace,
        Agent agent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all checkpoints for an agent
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of checkpoints</returns>
    Task<List<Checkpoint>> GetCheckpointsAsync(
        Guid agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint
    /// </summary>
    /// <param name="checkpoint">Checkpoint to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteCheckpointAsync(
        Checkpoint checkpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all checkpoints for an agent
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllCheckpointsAsync(
        Guid agentId,
        CancellationToken cancellationToken = default);
}
