using System;
using System.Collections.ObjectModel;

namespace Convalonia.Models;

/// <summary>
/// Represents a Claude agent working on a specific task
/// </summary>
public class Agent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public AgentStatus Status { get; set; }
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    public ObservableCollection<Message> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum AgentStatus
{
    Idle,
    Thinking,
    UsingTool,
    WaitingForUser,
    Completed,
    Error
}
