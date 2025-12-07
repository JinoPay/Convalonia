using System;
using System.Collections.ObjectModel;

namespace Convalonia.Models;

/// <summary>
/// Represents a source git repository (the original/parent repository)
/// Contains multiple workspaces as clones with different branches
/// </summary>
public class SourceRepository
{
    public Guid Id { get; set; }

    /// <summary>
    /// Repository name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Original repository path (for local repos) or URL (for remote repos)
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of source: Local or Remote
    /// </summary>
    public RepositorySourceType SourceType { get; set; }

    /// <summary>
    /// Default branch to use when creating new workspaces
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Workspaces (clones) created from this repository
    /// </summary>
    public ObservableCollection<Workspace> Workspaces { get; set; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last accessed timestamp
    /// </summary>
    public DateTime LastAccessedAt { get; set; }
}

public enum RepositorySourceType
{
    Local,
    Remote
}
