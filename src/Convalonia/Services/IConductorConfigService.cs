using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Service for managing conductor.json configuration files
/// </summary>
public interface IConductorConfigService
{
    /// <summary>
    /// Loads conductor.json from a workspace path
    /// </summary>
    /// <param name="workspacePath">Path to the workspace directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ConductorConfig object, or null if file doesn't exist</returns>
    Task<ConductorConfig?> LoadConfigAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves conductor.json to a workspace path
    /// </summary>
    /// <param name="workspacePath">Path to the workspace directory</param>
    /// <param name="config">Configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigAsync(string workspacePath, ConductorConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if conductor.json exists in a workspace
    /// </summary>
    /// <param name="workspacePath">Path to the workspace directory</param>
    /// <returns>True if conductor.json exists</returns>
    bool ConfigExists(string workspacePath);

    /// <summary>
    /// Creates a default conductor.json template
    /// </summary>
    /// <returns>Default configuration</returns>
    ConductorConfig CreateDefaultConfig();
}
