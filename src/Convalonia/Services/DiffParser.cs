using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Parses Git diff output into structured FileDiff objects
/// </summary>
public static class DiffParser
{
    private static readonly Regex FileDiffHeaderRegex = new(@"^diff --git a/(.*) b/(.*)$", RegexOptions.Compiled);
    private static readonly Regex FileRenameRegex = new(@"^rename from (.*)$", RegexOptions.Compiled);
    private static readonly Regex FileNewModeRegex = new(@"^new file mode", RegexOptions.Compiled);
    private static readonly Regex FileDeletedModeRegex = new(@"^deleted file mode", RegexOptions.Compiled);
    private static readonly Regex HunkHeaderRegex = new(@"^@@ -(\d+),?(\d*) \+(\d+),?(\d*) @@(.*)$", RegexOptions.Compiled);

    /// <summary>
    /// Parses Git diff output into structured FileDiff objects
    /// </summary>
    public static List<FileDiff> Parse(string diffOutput)
    {
        if (string.IsNullOrWhiteSpace(diffOutput))
            return new List<FileDiff>();

        var fileDiffs = new List<FileDiff>();
        var lines = diffOutput.Split('\n');

        FileDiff? currentFile = null;
        DiffHunk? currentHunk = null;
        int oldLineNumber = 0;
        int newLineNumber = 0;

        foreach (var line in lines)
        {
            // New file diff header
            var fileDiffMatch = FileDiffHeaderRegex.Match(line);
            if (fileDiffMatch.Success)
            {
                if (currentFile != null)
                {
                    fileDiffs.Add(currentFile);
                }

                var oldPath = fileDiffMatch.Groups[1].Value;
                var newPath = fileDiffMatch.Groups[2].Value;

                currentFile = new FileDiff
                {
                    FilePath = newPath,
                    ChangeType = FileChangeType.Modified,
                    Hunks = new List<DiffHunk>()
                };
                currentHunk = null;
                continue;
            }

            if (currentFile == null)
                continue;

            // File mode changes
            if (FileNewModeRegex.IsMatch(line))
            {
                currentFile = currentFile with { ChangeType = FileChangeType.Added };
                continue;
            }

            if (FileDeletedModeRegex.IsMatch(line))
            {
                currentFile = currentFile with { ChangeType = FileChangeType.Deleted };
                continue;
            }

            // File rename
            var renameMatch = FileRenameRegex.Match(line);
            if (renameMatch.Success)
            {
                currentFile = currentFile with
                {
                    ChangeType = FileChangeType.Renamed,
                    OldFilePath = renameMatch.Groups[1].Value
                };
                continue;
            }

            // Hunk header
            var hunkMatch = HunkHeaderRegex.Match(line);
            if (hunkMatch.Success)
            {
                if (currentHunk != null && currentFile.Hunks is List<DiffHunk> hunks)
                {
                    hunks.Add(currentHunk);
                }

                oldLineNumber = int.Parse(hunkMatch.Groups[1].Value);
                newLineNumber = int.Parse(hunkMatch.Groups[3].Value);

                currentHunk = new DiffHunk
                {
                    Header = line,
                    Lines = new List<DiffLine>()
                };
                continue;
            }

            // Diff line content
            if (currentHunk != null && line.Length > 0)
            {
                var lineType = line[0] switch
                {
                    '+' => DiffLineType.Added,
                    '-' => DiffLineType.Deleted,
                    ' ' => DiffLineType.Context,
                    _ => DiffLineType.Context
                };

                var content = line.Length > 1 ? line[1..] : string.Empty;

                var diffLine = new DiffLine
                {
                    Type = lineType,
                    Content = content,
                    OldLineNumber = lineType != DiffLineType.Added ? oldLineNumber : null,
                    NewLineNumber = lineType != DiffLineType.Deleted ? newLineNumber : null
                };

                if (currentHunk.Lines is List<DiffLine> hunkLines)
                {
                    hunkLines.Add(diffLine);
                }

                // Update line numbers
                if (lineType == DiffLineType.Added)
                {
                    newLineNumber++;
                }
                else if (lineType == DiffLineType.Deleted)
                {
                    oldLineNumber++;
                }
                else // Context
                {
                    oldLineNumber++;
                    newLineNumber++;
                }
            }
        }

        // Add last file and hunk
        if (currentHunk != null && currentFile != null && currentFile.Hunks is List<DiffHunk> lastHunks)
        {
            lastHunks.Add(currentHunk);
        }

        if (currentFile != null)
        {
            fileDiffs.Add(currentFile);
        }

        // Calculate added/deleted lines
        return fileDiffs.Select(file => file with
        {
            AddedLines = file.Hunks.SelectMany(h => h.Lines).Count(l => l.Type == DiffLineType.Added),
            DeletedLines = file.Hunks.SelectMany(h => h.Lines).Count(l => l.Type == DiffLineType.Deleted)
        }).ToList();
    }

    /// <summary>
    /// Parses git status --porcelain output to get file change types
    /// </summary>
    public static List<(string FilePath, FileChangeType ChangeType)> ParseStatus(string statusOutput)
    {
        if (string.IsNullOrWhiteSpace(statusOutput))
            return new List<(string, FileChangeType)>();

        var result = new List<(string, FileChangeType)>();
        var lines = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Length < 4)
                continue;

            var status = line[..2];
            var filePath = line[3..].Trim();

            // Handle renamed files (format: "R  oldpath -> newpath")
            if (status.Contains('R'))
            {
                var parts = filePath.Split(" -> ");
                if (parts.Length == 2)
                {
                    result.Add((parts[1], FileChangeType.Renamed));
                    continue;
                }
            }

            var changeType = status.Trim() switch
            {
                "A" or "??" => FileChangeType.Added,
                "D" => FileChangeType.Deleted,
                "M" or "MM" or " M" or "M " => FileChangeType.Modified,
                "R" => FileChangeType.Renamed,
                "C" => FileChangeType.Copied,
                _ => FileChangeType.Modified
            };

            result.Add((filePath, changeType));
        }

        return result;
    }
}
