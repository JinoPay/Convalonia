using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Convalonia.Services;

/// <summary>
/// Handles communication with Claude Code CLI
/// </summary>
public class ClaudeCodeService : IDisposable
{
    private Process? _process;
    private readonly string _workingDirectory;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;

    public bool IsRunning => _process != null && !_process.HasExited;

    public ClaudeCodeService(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Starts a Claude Code CLI session
    /// </summary>
    public async Task<bool> StartSessionAsync()
    {
        if (IsRunning)
        {
            return true;
        }

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

            _process = new Process { StartInfo = processInfo };

            // Setup output/error handlers
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await Task.Delay(500); // Give it time to initialize

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Claude Code: {ex.Message}");
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
            Console.WriteLine($"Failed to send message: {ex.Message}");
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
        if (_process == null || _process.HasExited)
        {
            return;
        }

        try
        {
            // Send exit command
            if (_process.StandardInput != null)
            {
                await _process.StandardInput.WriteLineAsync("/exit");
                await _process.StandardInput.FlushAsync();
            }

            // Wait for graceful exit
            if (!_process.WaitForExit(3000))
            {
                _process.Kill();
            }

            _process.Dispose();
            _process = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping Claude Code: {ex.Message}");
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
        StopSessionAsync().Wait();
    }
}
