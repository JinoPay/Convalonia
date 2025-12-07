using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;
using Microsoft.Extensions.Logging;

namespace Convalonia.Services;

/// <summary>
/// Executes conductor.json scripts with proper environment variables
/// </summary>
public class ScriptExecutor : IScriptExecutor
{
    private readonly IConductorConfigService _configService;
    private readonly IPortAllocator _portAllocator;
    private readonly ILogger<ScriptExecutor> _logger;
    private readonly Dictionary<Guid, Process> _runningProcesses = new();
    private readonly object _lock = new();

    public ScriptExecutor(
        IConductorConfigService configService,
        IPortAllocator portAllocator,
        ILogger<ScriptExecutor> logger)
    {
        _configService = configService;
        _portAllocator = portAllocator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteSetupScriptAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var config = await _configService.LoadConfigAsync(workspace.Path, cancellationToken);
        if (config?.Scripts?.Setup == null)
        {
            _logger.LogDebug("No setup script defined for workspace {WorkspaceName}", workspace.Name);
            return;
        }

        _logger.LogInformation("Executing setup script for workspace {WorkspaceName}", workspace.Name);

        var env = BuildEnvironmentVariables(workspace, config);
        await RunScriptAsync(config.Scripts.Setup, workspace.Path, env, config.Shell, cancellationToken);

        _logger.LogInformation("Setup script completed for workspace {WorkspaceName}", workspace.Name);
    }

    /// <inheritdoc />
    public async Task<Process?> ExecuteRunScriptAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var config = await _configService.LoadConfigAsync(workspace.Path, cancellationToken);
        if (config?.Scripts?.Run == null)
        {
            _logger.LogDebug("No run script defined for workspace {WorkspaceName}", workspace.Name);
            return null;
        }

        // Handle nonconcurrent mode
        if (config.RunScriptMode == "nonconcurrent")
        {
            lock (_lock)
            {
                // Stop any currently running script
                var runningProcess = _runningProcesses.Values.FirstOrDefault(p => p != null && !p.HasExited);
                if (runningProcess != null)
                {
                    _logger.LogInformation("Stopping previous run script (nonconcurrent mode)");
                    try
                    {
                        runningProcess.Kill(entireProcessTree: true);
                        runningProcess.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to kill previous process");
                    }
                }
            }
        }

        _logger.LogInformation("Executing run script for workspace {WorkspaceName}", workspace.Name);

        var env = BuildEnvironmentVariables(workspace, config);
        var process = await RunScriptInBackgroundAsync(config.Scripts.Run, workspace.Path, env, config.Shell, cancellationToken);

        if (process != null)
        {
            lock (_lock)
            {
                _runningProcesses[workspace.Id] = process;
            }
        }

        return process;
    }

    /// <inheritdoc />
    public async Task ExecuteArchiveScriptAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var config = await _configService.LoadConfigAsync(workspace.Path, cancellationToken);
        if (config?.Scripts?.Archive == null)
        {
            _logger.LogDebug("No archive script defined for workspace {WorkspaceName}", workspace.Name);
            return;
        }

        _logger.LogInformation("Executing archive script for workspace {WorkspaceName}", workspace.Name);

        var env = BuildEnvironmentVariables(workspace, config);
        await RunScriptAsync(config.Scripts.Archive, workspace.Path, env, config.Shell, cancellationToken);

        _logger.LogInformation("Archive script completed for workspace {WorkspaceName}", workspace.Name);
    }

    /// <inheritdoc />
    public void StopRunScript(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_runningProcesses.TryGetValue(workspaceId, out var process))
            {
                if (process != null && !process.HasExited)
                {
                    try
                    {
                        _logger.LogInformation("Stopping run script for workspace {WorkspaceId}", workspaceId);
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to stop process for workspace {WorkspaceId}", workspaceId);
                    }
                }

                _runningProcesses.Remove(workspaceId);
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunScriptRunning(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_runningProcesses.TryGetValue(workspaceId, out var process))
            {
                return process != null && !process.HasExited;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public Process? GetRunningProcess(Guid workspaceId)
    {
        lock (_lock)
        {
            if (_runningProcesses.TryGetValue(workspaceId, out var process))
            {
                return process != null && !process.HasExited ? process : null;
            }
            return null;
        }
    }

    /// <summary>
    /// Builds environment variables for script execution
    /// </summary>
    private Dictionary<string, string> BuildEnvironmentVariables(Workspace workspace, ConductorConfig config)
    {
        var env = new Dictionary<string, string>();

        // Add system environment variables
        foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
        {
            env[envVar.Key.ToString()!] = envVar.Value?.ToString() ?? string.Empty;
        }

        // Allocate port for workspace
        var basePort = _portAllocator.AllocatePort(workspace.Id);

        // Add Conductor-specific environment variables
        env["CONDUCTOR_WORKSPACE_PATH"] = workspace.Path;
        env["CONDUCTOR_WORKSPACE_NAME"] = workspace.Name;
        env["CONDUCTOR_PORT"] = basePort.ToString();

        // Add root path if available (first repository)
        if (workspace.Repositories.Count > 0)
        {
            env["CONDUCTOR_ROOT_PATH"] = workspace.Repositories[0].RootPath ?? workspace.Repositories[0].WorkspacePath;
        }

        // Add custom environment variables from config
        if (config.Env != null)
        {
            foreach (var kvp in config.Env)
            {
                env[kvp.Key] = kvp.Value;
            }
        }

        return env;
    }

    /// <summary>
    /// Runs a script and waits for completion
    /// </summary>
    private async Task RunScriptAsync(
        string script,
        string workingDirectory,
        Dictionary<string, string> environmentVariables,
        string? shell,
        CancellationToken cancellationToken)
    {
        var (fileName, arguments) = GetShellCommand(script, shell);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Set environment variables
        foreach (var kvp in environmentVariables)
        {
            startInfo.Environment[kvp.Key] = kvp.Value;
        }

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.LogInformation("[Script Output] {Data}", args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.LogWarning("[Script Error] {Data}", args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Script failed with exit code {process.ExitCode}");
        }
    }

    /// <summary>
    /// Runs a script in background and returns the process
    /// </summary>
    private Task<Process?> RunScriptInBackgroundAsync(
        string script,
        string workingDirectory,
        Dictionary<string, string> environmentVariables,
        string? shell,
        CancellationToken cancellationToken)
    {
        var (fileName, arguments) = GetShellCommand(script, shell);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Set environment variables
        foreach (var kvp in environmentVariables)
        {
            startInfo.Environment[kvp.Key] = kvp.Value;
        }

        var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.LogInformation("[Run Script] {Data}", args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.LogWarning("[Run Script Error] {Data}", args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return Task.FromResult<Process?>(process);
    }

    /// <summary>
    /// Gets the appropriate shell command for the platform
    /// </summary>
    private (string fileName, string arguments) GetShellCommand(string script, string? customShell)
    {
        if (!string.IsNullOrEmpty(customShell))
        {
            return (customShell, $"-c \"{EscapeShellArgument(script)}\"");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("cmd.exe", $"/c \"{EscapeShellArgument(script)}\"");
        }
        else
        {
            // macOS and Linux
            return ("/bin/bash", $"-c \"{EscapeShellArgument(script)}\"");
        }
    }

    /// <summary>
    /// Escapes shell arguments to prevent injection
    /// </summary>
    private string EscapeShellArgument(string argument)
    {
        // Basic escaping - replace double quotes with escaped quotes
        return argument.Replace("\"", "\\\"");
    }
}
