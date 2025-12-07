using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for persisting agent conversation history
/// </summary>
public interface IAgentPersistenceService
{
    /// <summary>
    /// Save agent conversation history to persistent storage
    /// </summary>
    Task SaveAgentMessagesAsync(Agent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load agent conversation history from persistent storage
    /// </summary>
    Task<IEnumerable<Message>> LoadAgentMessagesAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete agent conversation history from persistent storage
    /// </summary>
    Task DeleteAgentMessagesAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save last active agent ID for a workspace
    /// </summary>
    Task SaveLastActiveAgentAsync(Guid workspaceId, Guid? agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get last active agent ID for a workspace
    /// </summary>
    Task<Guid?> GetLastActiveAgentAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
