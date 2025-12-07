using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Convalonia.Services;

/// <summary>
/// Allocates unique port ranges for workspaces
/// Each workspace gets 10 ports (base port + 0 to 9)
/// </summary>
public class PortAllocator : IPortAllocator
{
    private readonly ILogger<PortAllocator> _logger;
    private readonly Dictionary<Guid, int> _workspacePorts = new();
    private readonly object _lock = new();

    private const int BasePort = 3000;
    private const int PortRangeSize = 10;

    public PortAllocator(ILogger<PortAllocator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public int AllocatePort(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_workspacePorts.TryGetValue(workspaceId, out var existingPort))
            {
                _logger.LogDebug("Workspace {WorkspaceId} already has port {Port}", workspaceId, existingPort);
                return existingPort;
            }

            // Allocate new port range
            var basePort = BasePort + (_workspacePorts.Count * PortRangeSize);
            _workspacePorts[workspaceId] = basePort;

            _logger.LogInformation("Allocated port range {BasePort}-{MaxPort} for workspace {WorkspaceId}",
                basePort, basePort + PortRangeSize - 1, workspaceId);

            return basePort;
        }
    }

    /// <inheritdoc />
    public int GetPort(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_workspacePorts.TryGetValue(workspaceId, out var port))
            {
                return port;
            }

            throw new InvalidOperationException($"No port allocated for workspace {workspaceId}");
        }
    }

    /// <inheritdoc />
    public void ReleasePort(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_workspacePorts.Remove(workspaceId))
            {
                _logger.LogInformation("Released port for workspace {WorkspaceId}", workspaceId);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, int> GetAllAllocations()
    {
        lock (_lock)
        {
            return new Dictionary<Guid, int>(_workspacePorts);
        }
    }

    /// <inheritdoc />
    public int[] GetPortRange(Guid workspaceId)
    {
        var basePort = GetPort(workspaceId);
        return Enumerable.Range(basePort, PortRangeSize).ToArray();
    }
}
