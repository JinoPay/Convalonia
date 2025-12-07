using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;
using Microsoft.Extensions.Logging;

namespace Convalonia.Services;

/// <summary>
/// Service for managing conductor.json configuration files
/// </summary>
public class ConductorConfigService : IConductorConfigService
{
    private readonly ILogger<ConductorConfigService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ConfigFileName = "conductor.json";

    public ConductorConfigService(ILogger<ConductorConfigService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<ConductorConfig?> LoadConfigAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var configPath = GetConfigPath(workspacePath);

            if (!File.Exists(configPath))
            {
                _logger.LogDebug("conductor.json not found at {Path}", configPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<ConductorConfig>(json, _jsonOptions);

            _logger.LogInformation("Loaded conductor.json from {Path}", configPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load conductor.json from {Path}", workspacePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveConfigAsync(string workspacePath, ConductorConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var configPath = GetConfigPath(workspacePath);
            var json = JsonSerializer.Serialize(config, _jsonOptions);

            await File.WriteAllTextAsync(configPath, json, cancellationToken);

            _logger.LogInformation("Saved conductor.json to {Path}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save conductor.json to {Path}", workspacePath);
            throw;
        }
    }

    /// <inheritdoc />
    public bool ConfigExists(string workspacePath)
    {
        var configPath = GetConfigPath(workspacePath);
        return File.Exists(configPath);
    }

    /// <inheritdoc />
    public ConductorConfig CreateDefaultConfig()
    {
        return new ConductorConfig
        {
            Scripts = new ConductorScripts
            {
                Setup = "npm install",
                Run = "npm run dev",
                Archive = null
            },
            RunScriptMode = "nonconcurrent",
            Shell = null, // Use system default
            Env = null
        };
    }

    private string GetConfigPath(string workspacePath)
    {
        // Validate path to prevent path traversal
        var fullPath = Path.GetFullPath(workspacePath);
        return Path.Combine(fullPath, ConfigFileName);
    }
}
