using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Models;
using Convalonia.Services;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Toast;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for chat interface with a Claude agent
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly Agent _agent;
    private readonly ClaudeCodeService _claudeCodeService;
    private readonly IToastService _toastService;
    private readonly string _workspacePath;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isFirstMessage = true;

    [ObservableProperty]
    private ObservableCollection<Message> _messages;

    [ObservableProperty]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isSending = false;

    [ObservableProperty]
    private AgentStatus _agentStatus;

    [ObservableProperty]
    private string _agentName;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private string _terminalOutput = string.Empty;

    [ObservableProperty]
    private bool _showTerminal = true;

    /// <summary>
    /// Event raised when the first user message is sent
    /// Used to trigger workspace auto-renaming
    /// </summary>
    public event EventHandler<string>? FirstMessageSent;

    public ChatViewModel(
        Agent agent,
        string workspacePath,
        IToastService toastService,
        IClaudeCodeServiceFactory claudeCodeServiceFactory)
    {
        _agent = agent;
        _workspacePath = workspacePath;
        _toastService = toastService;
        _claudeCodeService = claudeCodeServiceFactory.Create(workspacePath);

        // Subscribe to terminal output
        _claudeCodeService.OutputReceived += OnOutputReceived;
        _claudeCodeService.ErrorReceived += OnErrorReceived;

        _messages = agent.Messages;
        _agentName = agent.Name;
        _agentStatus = agent.Status;

        UpdateStatusText();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;

        if (IsSending)
            return;

        IsSending = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Start Claude Code session if not already running
            if (!_claudeCodeService.IsRunning)
            {
                var started = await _claudeCodeService.StartSessionAsync();
                if (!started)
                {
                    _toastService.ShowError("Failed to start Claude Code CLI");
                    return;
                }
                _toastService.ShowSuccess("Claude Code session started");
            }

            // Add user message
            var userMessage = new Message
            {
                Id = Guid.NewGuid(),
                Role = MessageRole.User,
                Content = InputMessage,
                Timestamp = DateTime.Now
            };

            _messages.Add(userMessage);
            var userInput = InputMessage;
            InputMessage = string.Empty;

            // Trigger first message event for workspace auto-renaming
            if (_isFirstMessage)
            {
                _isFirstMessage = false;
                FirstMessageSent?.Invoke(this, userInput);
            }

            // Update agent status
            _agent.Status = AgentStatus.Thinking;
            AgentStatus = _agent.Status;
            UpdateStatusText();

            // Send to Claude Code CLI
            var sent = await _claudeCodeService.SendMessageAsync(userInput, _cancellationTokenSource.Token);

            if (!sent)
            {
                _toastService.ShowError("Failed to send message to Claude Code");
                _agent.Status = AgentStatus.Error;
                AgentStatus = _agent.Status;
                UpdateStatusText();
                return;
            }

            // Wait a bit for response to accumulate
            await Task.Delay(1000, _cancellationTokenSource.Token);

            // Update agent status
            _agent.Status = AgentStatus.Idle;
            AgentStatus = _agent.Status;
            UpdateStatusText();
        }
        catch (OperationCanceledException)
        {
            _toastService.ShowInfo("Message cancelled");
            _agent.Status = AgentStatus.Idle;
            AgentStatus = _agent.Status;
            UpdateStatusText();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to send message: {ex.Message}");
            _agent.Status = AgentStatus.Error;
            AgentStatus = _agent.Status;
            UpdateStatusText();
        }
        finally
        {
            IsSending = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelSending()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private async Task ClearChatAsync()
    {
        _messages.Clear();
        _claudeCodeService.ClearBuffers();
        TerminalOutput = string.Empty;
        _agent.Status = AgentStatus.Idle;
        AgentStatus = _agent.Status;
        UpdateStatusText();
        _toastService.ShowInfo("Chat cleared");

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        ShowTerminal = !ShowTerminal;
    }

    [RelayCommand]
    private async Task StopSessionAsync()
    {
        await _claudeCodeService.StopSessionAsync();
        _toastService.ShowInfo("Claude Code session stopped");
        _agent.Status = AgentStatus.Idle;
        AgentStatus = _agent.Status;
        UpdateStatusText();
    }

    private void OnOutputReceived(object? sender, string output)
    {
        TerminalOutput += output + Environment.NewLine;
    }

    private void OnErrorReceived(object? sender, string error)
    {
        TerminalOutput += $"[ERROR] {error}" + Environment.NewLine;
    }

    [RelayCommand]
    private async Task RegenerateResponseAsync()
    {
        if (_messages.Count < 1)
            return;

        // Get the last user message and resend
        var lastUserMessage = _messages.LastOrDefault(m => m.Role == MessageRole.User);
        if (lastUserMessage != null)
        {
            InputMessage = lastUserMessage.Content;
            await SendMessageAsync();
        }
    }

    public new void Dispose()
    {
        _claudeCodeService.OutputReceived -= OnOutputReceived;
        _claudeCodeService.ErrorReceived -= OnErrorReceived;
        _claudeCodeService.Dispose();
        base.Dispose();
    }

    private void UpdateStatusText()
    {
        StatusText = AgentStatus switch
        {
            AgentStatus.Idle => "Idle",
            AgentStatus.Thinking => "Thinking...",
            AgentStatus.UsingTool => "Using tool...",
            AgentStatus.WaitingForUser => "Waiting for input",
            AgentStatus.Completed => "Completed",
            AgentStatus.Error => "Error",
            _ => "Unknown"
        };
    }
}
