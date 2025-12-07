using System.Collections.Generic;

namespace Convalonia.Models;

/// <summary>
/// Represents a file diff in a Git repository
/// </summary>
public record FileDiff
{
    /// <summary>
    /// File path relative to repository root
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Change type (Added, Modified, Deleted, Renamed)
    /// </summary>
    public FileChangeType ChangeType { get; init; }

    /// <summary>
    /// Number of lines added
    /// </summary>
    public int AddedLines { get; init; }

    /// <summary>
    /// Number of lines deleted
    /// </summary>
    public int DeletedLines { get; init; }

    /// <summary>
    /// Diff hunks (sections of changes)
    /// </summary>
    public List<DiffHunk> Hunks { get; init; } = new();

    /// <summary>
    /// Old file path (for renamed files)
    /// </summary>
    public string? OldFilePath { get; init; }
}

/// <summary>
/// Represents a section of changes in a file
/// </summary>
public class DiffHunk
{
    /// <summary>
    /// Header line (e.g., "@@ -1,5 +1,7 @@")
    /// </summary>
    public string Header { get; init; } = string.Empty;

    /// <summary>
    /// Lines in this hunk
    /// </summary>
    public List<DiffLine> Lines { get; init; } = new();
}

/// <summary>
/// Represents a single line in a diff
/// </summary>
public class DiffLine
{
    /// <summary>
    /// Line type (Added, Deleted, Context)
    /// </summary>
    public DiffLineType Type { get; init; }

    /// <summary>
    /// Line content (without +/- prefix)
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Old line number (for context and deleted lines)
    /// </summary>
    public int? OldLineNumber { get; init; }

    /// <summary>
    /// New line number (for context and added lines)
    /// </summary>
    public int? NewLineNumber { get; init; }
}

/// <summary>
/// Type of file change
/// </summary>
public enum FileChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied
}

/// <summary>
/// Type of diff line
/// </summary>
public enum DiffLineType
{
    Context,  // Unchanged line (context)
    Added,    // Added line (+)
    Deleted   // Deleted line (-)
}
