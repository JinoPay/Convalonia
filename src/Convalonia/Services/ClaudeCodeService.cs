using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Convalonia.Services;

/// <summary>
/// Handles communication with Claude Code CLI
/// </summary>
public class ClaudeCodeService : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<ClaudeCodeService>();
    private Process? _process;
    private readonly string _workingDirectory;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();
    private readonly object _processLock = new();
    private bool _isDisposed;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;

    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                return _process != null && !_process.HasExited;
            }
        }
    }

    public ClaudeCodeService(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Checks if Claude Code CLI is installed and available
    /// </summary>
    public static async Task<bool> IsClaudeCodeInstalledAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Starts a Claude Code CLI session
    /// </summary>
    public async Task<bool> StartSessionAsync()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ClaudeCodeService));
        }

        lock (_processLock)
        {
            if (_process != null && !_process.HasExited)
            {
                return true;
            }
        }

        Process? newProcess = null;
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = "chat",
                WorkingDirectory = _workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            newProcess = new Process { StartInfo = processInfo };

            // Setup output/error handlers
            newProcess.OutputDataReceived += OnOutputDataReceived;
            newProcess.ErrorDataReceived += OnErrorDataReceived;

            newProcess.Start();
            newProcess.BeginOutputReadLine();
            newProcess.BeginErrorReadLine();

            lock (_processLock)
            {
                _process = newProcess;
                newProcess = null; // Ownership transferred
            }

            await Task.Delay(500); // Give it time to initialize

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start Claude Code in {WorkingDirectory}", _workingDirectory);
            newProcess?.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Sends a message to Claude Code CLI
    /// </summary>
    public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _process?.StandardInput == null)
        {
            return false;
        }

        try
        {
            await _process.StandardInput.WriteLineAsync(message);
            await _process.StandardInput.FlushAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send message to Claude Code");
            return false;
        }
    }

    /// <summary>
    /// Gets all output received so far
    /// </summary>
    public string GetOutput()
    {
        lock (_outputBuffer)
        {
            return _outputBuffer.ToString();
        }
    }

    /// <summary>
    /// Gets all error output received so far
    /// </summary>
    public string GetErrors()
    {
        lock (_errorBuffer)
        {
            return _errorBuffer.ToString();
        }
    }

    /// <summary>
    /// Clears the output buffers
    /// </summary>
    public void ClearBuffers()
    {
        lock (_outputBuffer)
        {
            _outputBuffer.Clear();
        }
        lock (_errorBuffer)
        {
            _errorBuffer.Clear();
        }
    }

    /// <summary>
    /// Stops the Claude Code CLI session
    /// </summary>
    public async Task StopSessionAsync()
    {
        Process? processToDispose = null;

        lock (_processLock)
        {
            if (_process == null || _process.HasExited)
            {
                return;
            }
            processToDispose = _process;
            _process = null;
        }

        try
        {
            // Send exit command
            if (processToDispose.StandardInput != null)
            {
                await processToDispose.StandardInput.WriteLineAsync("/exit");
                await processToDispose.StandardInput.FlushAsync();
            }

            // Wait for graceful exit
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                await processToDispose.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                processToDispose.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping Claude Code");
        }
        finally
        {
            processToDispose.Dispose();
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_outputBuffer)
            {
                _outputBuffer.AppendLine(e.Data);
            }
            OutputReceived?.Invoke(this, e.Data);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_errorBuffer)
            {
                _errorBuffer.AppendLine(e.Data);
            }
            ErrorReceived?.Invoke(this, e.Data);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            // Synchronously stop the session - avoid deadlock by using GetAwaiter().GetResult()
            // in a try block to handle any exceptions
            try
            {
                StopSessionAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during dispose of Claude Code service");
            }
        }

        _isDisposed = true;
    }

    ~ClaudeCodeService()
    {
        Dispose(disposing: false);
    }
}
