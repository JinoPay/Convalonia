using System;

namespace Convalonia.Models;

/// <summary>
/// Represents a checkpoint (snapshot) of workspace state at a specific turn
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// Unique identifier for this checkpoint
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// ID of the workspace this checkpoint belongs to
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// ID of the agent this checkpoint belongs to
    /// </summary>
    public Guid AgentId { get; init; }

    /// <summary>
    /// Turn number (message pair index)
    /// </summary>
    public int TurnNumber { get; init; }

    /// <summary>
    /// Git commit SHA for this checkpoint
    /// </summary>
    public string GitCommitSha { get; init; } = string.Empty;

    /// <summary>
    /// When this checkpoint was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// User message that triggered this checkpoint
    /// </summary>
    public string UserMessage { get; init; } = string.Empty;

    /// <summary>
    /// Assistant response for this checkpoint
    /// </summary>
    public string AssistantMessage { get; init; } = string.Empty;

    /// <summary>
    /// Git reference name for this checkpoint
    /// </summary>
    public string RefName => $"refs/conductor/checkpoints/{WorkspaceId}/{AgentId}/turn-{TurnNumber}";
}
