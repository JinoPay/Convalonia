using System;
using System.Collections.ObjectModel;

namespace Convalonia.Models;

/// <summary>
/// Represents a workspace where Claude agents can work on tasks
/// </summary>
public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Legacy properties for backwards compatibility
    /// </summary>
    public string? GitBranch { get; set; }
    public string? GitRemote { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public WorkspaceStatus Status { get; set; }
    public ObservableCollection<Agent> Agents { get; set; } = new();

    /// <summary>
    /// Multiple repositories within this workspace
    /// </summary>
    public ObservableCollection<Repository> Repositories { get; set; } = new();
}

public enum WorkspaceStatus
{
    Idle,
    Working,
    Error,
    Completed
}
