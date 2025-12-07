using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for chat interface with a Claude agent
/// </summary>
public partial class ChatViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private readonly Agent _agent;
    private readonly Workspace _workspace;
    private readonly ClaudeCodeService _claudeCodeService;
    private readonly IToastService _toastService;
    private readonly ICheckpointService? _checkpointService;
    private readonly IAgentPersistenceService? _agentPersistence;
    private readonly string _workspacePath;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isFirstMessage = true;
    private int _currentTurn = 0;

    public ViewModelActivator Activator { get; }

    [Reactive]
    private ObservableCollection<Message> _messages;

    [Reactive]
    private string _inputMessage = string.Empty;

    [Reactive]
    private bool _isSending = false;

    [Reactive]
    private AgentStatus _agentStatus;

    [Reactive]
    private string _agentName;

    [Reactive]
    private string _statusText = "Idle";

    [Reactive]
    private string _terminalOutput = string.Empty;

    [Reactive]
    private bool _showTerminal = true;

    /// <summary>
    /// Exposes the underlying agent for data binding
    /// </summary>
    public Agent Agent => _agent;

    /// <summary>
    /// Event raised when the first user message is sent
    /// Used to trigger workspace auto-renaming
    /// </summary>
    public event EventHandler<string>? FirstMessageSent;

    public ChatViewModel(
        Agent agent,
        Workspace workspace,
        IToastService toastService,
        IClaudeCodeServiceFactory claudeCodeServiceFactory,
        ICheckpointService? checkpointService = null,
        IAgentPersistenceService? agentPersistence = null)
    {
        _agent = agent;
        _workspace = workspace;
        _workspacePath = workspace.Path;
        _toastService = toastService;
        _claudeCodeService = claudeCodeServiceFactory.Create(workspace.Path);
        _checkpointService = checkpointService;
        _agentPersistence = agentPersistence;

        Activator = new ViewModelActivator();

        _messages = agent.Messages;
        _agentName = agent.Name;
        _agentStatus = agent.Status;

        // Subscribe to message changes for auto-save
        _messages.CollectionChanged += OnMessagesCollectionChanged;

        UpdateStatusText();

        // Set up activator for lifecycle management
        this.WhenActivated(disposables =>
        {
            // Subscribe to terminal output
            disposables.Add(Observable.FromEventPattern<string>(
                h => _claudeCodeService.OutputReceived += h,
                h => _claudeCodeService.OutputReceived -= h)
                .Subscribe(evt => OnOutputReceived(this, evt.EventArgs)));

            disposables.Add(Observable.FromEventPattern<string>(
                h => _claudeCodeService.ErrorReceived += h,
                h => _claudeCodeService.ErrorReceived -= h)
                .Subscribe(evt => OnErrorReceived(this, evt.EventArgs)));

            disposables.Add(Disposable.Create(() =>
            {
                _claudeCodeService.Dispose();
            }));
        });
    }

    /// <summary>
    /// Auto-save messages when collection changes
    /// </summary>
    private async void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_agentPersistence != null)
        {
            try
            {
                await _agentPersistence.SaveAgentMessagesAsync(_agent);
            }
            catch (Exception ex)
            {
                // Log but don't interrupt user flow
                System.Diagnostics.Debug.WriteLine($"Failed to auto-save messages: {ex.Message}");
            }
        }
    }

    [ReactiveCommand]
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
                Timestamp = DateTime.Now,
                TurnNumber = _currentTurn
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

            // Create checkpoint after successful turn
            if (_checkpointService != null)
            {
                try
                {
                    // Get the last assistant message
                    var assistantMessage = _messages.LastOrDefault(m => m.Role == MessageRole.Assistant);
                    var assistantContent = assistantMessage?.Content ?? string.Empty;

                    await _checkpointService.CreateCheckpointAsync(
                        _workspace,
                        _agent,
                        _currentTurn,
                        userInput,
                        assistantContent,
                        _cancellationTokenSource.Token);

                    _currentTurn++;
                }
                catch (Exception ex)
                {
                    // Don't fail the whole operation if checkpoint fails
                    _toastService.ShowWarning($"Checkpoint creation failed: {ex.Message}");
                }
            }

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

    [ReactiveCommand]
    private void CancelSending()
    {
        _cancellationTokenSource?.Cancel();
    }

    [ReactiveCommand]
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

    [ReactiveCommand]
    private void ToggleTerminal()
    {
        ShowTerminal = !ShowTerminal;
    }

    [ReactiveCommand]
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

    [ReactiveCommand]
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

    public void Dispose()
    {
        _messages.CollectionChanged -= OnMessagesCollectionChanged;
        _cancellationTokenSource?.Dispose();
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

    /// <summary>
    /// Reverts to a specific checkpoint (turn)
    /// </summary>
    [ReactiveCommand(CanExecute = nameof(CanRevertToCheckpoint))]
    private async Task RevertToCheckpointAsync(int turnNumber)
    {
        if (_checkpointService == null)
        {
            _toastService.ShowWarning("Checkpoint service not available");
            return;
        }

        try
        {
            // Get the checkpoint for this turn
            var checkpoints = await _checkpointService.GetCheckpointsAsync(_agent.Id);
            var checkpoint = checkpoints.FirstOrDefault(c => c.TurnNumber == turnNumber);

            if (checkpoint == null)
            {
                _toastService.ShowWarning($"Checkpoint for turn {turnNumber} not found");
                return;
            }

            // Confirm with user
            var confirmed = true; // TODO: Add confirmation dialog
            if (!confirmed)
                return;

            _toastService.ShowInfo($"Reverting to turn {turnNumber}...");

            // Revert to checkpoint
            await _checkpointService.RevertToCheckpointAsync(checkpoint, _workspace, _agent);

            // Remove messages after the checkpoint
            var messagesToRemove = Messages
                .Where((m, index) => index >= turnNumber * 2) // Each turn has 2 messages (user + assistant)
                .ToList();

            foreach (var message in messagesToRemove)
            {
                Messages.Remove(message);
            }

            // Update current turn
            _currentTurn = turnNumber;

            _toastService.ShowSuccess($"Reverted to turn {turnNumber}");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to revert: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a checkpoint can be reverted to
    /// </summary>
    private bool CanRevertToCheckpoint(int turnNumber)
    {
        return turnNumber < _currentTurn && !IsSending;
    }
}
