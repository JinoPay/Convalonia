using System;

namespace Convalonia.Models;

/// <summary>
/// Represents a git repository within a workspace
/// </summary>
public class Repository
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Root path of the original git repository (if copied from local)
    /// </summary>
    public string? RootPath { get; set; }

    /// <summary>
    /// Path within the workspace where this repository lives
    /// </summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// Current branch name
    /// </summary>
    public string? CurrentBranch { get; set; }

    /// <summary>
    /// Branch to create new workspace from
    /// </summary>
    public string? BaseBranch { get; set; }

    /// <summary>
    /// Remote origin URL
    /// </summary>
    public string? RemoteOrigin { get; set; }

    /// <summary>
    /// Whether to include archived branches in search
    /// </summary>
    public bool SearchArchivedBranches { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last commit hash
    /// </summary>
    public string? LastCommitHash { get; set; }

    /// <summary>
    /// Whether this repository has uncommitted changes
    /// </summary>
    public bool HasChanges { get; set; }
}
