using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Services.Validation;
using Serilog;

namespace Convalonia.Services;

/// <summary>
/// Handles file system operations for workspaces
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILogger _logger = Log.ForContext<FileSystemService>();
    /// <summary>
    /// Reads a file from the workspace
    /// </summary>
    public async Task<string> ReadFileAsync(string filePath)
    {
        // Validate path
        if (!InputValidator.IsValidPath(filePath))
            throw new ValidationException("filePath", "Invalid file path");

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(filePath);
    }

    /// <summary>
    /// Writes content to a file in the workspace
    /// </summary>
    public async Task WriteFileAsync(string filePath, string content)
    {
        // Validate path
        if (!InputValidator.IsValidPath(filePath))
            throw new ValidationException("filePath", "Invalid file path");

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, content);
    }

    /// <summary>
    /// Lists files in a directory matching a pattern
    /// </summary>
    public Task<List<string>> ListFilesAsync(string directoryPath, string pattern = "*")
    {
        // Validate path
        if (!InputValidator.IsValidPath(directoryPath))
            throw new ValidationException("directoryPath", "Invalid directory path");

        if (!Directory.Exists(directoryPath))
            return Task.FromResult(new List<string>());

        var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(directoryPath, f))
            .ToList();

        return Task.FromResult(files);
    }

    /// <summary>
    /// Searches for files containing a specific pattern
    /// </summary>
    public async Task<List<FileMatch>> SearchInFilesAsync(string directoryPath, string searchPattern)
    {
        // Validate path
        if (!InputValidator.IsValidPath(directoryPath))
            throw new ValidationException("directoryPath", "Invalid directory path");

        var matches = new List<FileMatch>();

        if (!Directory.Exists(directoryPath))
            return matches;

        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new FileMatch
                        {
                            FilePath = Path.GetRelativePath(directoryPath, file),
                            LineNumber = i + 1,
                            LineContent = lines[i].Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error searching file {File}", file);
            }
        }

        return matches;
    }

    /// <summary>
    /// Deletes a file from the workspace
    /// </summary>
    public Task DeleteFileAsync(string filePath)
    {
        // Validate path
        if (!InputValidator.IsValidPath(filePath))
            throw new ValidationException("filePath", "Invalid file path");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
}

public class FileMatch
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string LineContent { get; set; } = string.Empty;
}
