using System;

namespace Convalonia.Models;

/// <summary>
/// Represents a task being executed by an agent
/// </summary>
public class Task
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public int Progress { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
