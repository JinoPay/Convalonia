using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Convalonia.Models;

/// <summary>
/// Represents the conductor.json configuration file format
/// </summary>
public class ConductorConfig
{
    /// <summary>
    /// Scripts to run at different lifecycle events
    /// </summary>
    [JsonPropertyName("scripts")]
    public ConductorScripts? Scripts { get; set; }

    /// <summary>
    /// Script execution mode: "nonconcurrent" prevents multiple run scripts from running simultaneously
    /// </summary>
    [JsonPropertyName("runScriptMode")]
    public string? RunScriptMode { get; set; }

    /// <summary>
    /// Default shell to use for script execution (bash, sh, zsh, etc.)
    /// </summary>
    [JsonPropertyName("shell")]
    public string? Shell { get; set; }

    /// <summary>
    /// Additional environment variables to set for all scripts
    /// </summary>
    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }
}

/// <summary>
/// Scripts that run at different lifecycle events of a workspace
/// </summary>
public class ConductorScripts
{
    /// <summary>
    /// Runs when a workspace is first created (e.g., npm install, .env setup)
    /// </summary>
    [JsonPropertyName("setup")]
    public string? Setup { get; set; }

    /// <summary>
    /// Runs when the user clicks the "Run" button (e.g., npm run dev)
    /// </summary>
    [JsonPropertyName("run")]
    public string? Run { get; set; }

    /// <summary>
    /// Runs when a workspace is archived/deleted (e.g., cleanup tasks)
    /// </summary>
    [JsonPropertyName("archive")]
    public string? Archive { get; set; }
}
