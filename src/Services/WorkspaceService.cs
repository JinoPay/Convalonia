using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Utils;

namespace Convalonia.Services;

/// <summary>
/// Manages workspaces for parallel Claude agent operations
/// </summary>
public class WorkspaceService
{
    private readonly ObservableCollection<Workspace> _workspaces = new();
    private readonly string _baseWorkspacePath;

    public WorkspaceService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "ConvaloniaWorkspaces"))
    {
    }

    public WorkspaceService(string baseWorkspacePath)
    {
        _baseWorkspacePath = baseWorkspacePath;

        // Create base workspace directory if it doesn't exist
        if (!Directory.Exists(_baseWorkspacePath))
        {
            Directory.CreateDirectory(_baseWorkspacePath);
        }
    }

    public ObservableCollection<Workspace> Workspaces => _workspaces;

    /// <summary>
    /// Creates a new workspace with an optional name (generates random name if not provided)
    /// </summary>
    public async Task<Workspace> CreateWorkspaceAsync(string? name = null, string? gitRemote = null)
    {
        // Generate random name if not provided
        var workspaceName = string.IsNullOrWhiteSpace(name)
            ? RandomNameGenerator.GenerateUnique(_workspaces.Select(w => w.Name))
            : name;

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = workspaceName,
            Path = Path.Combine(_baseWorkspacePath, SanitizeName(workspaceName)),
            GitRemote = gitRemote,
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now,
            Status = WorkspaceStatus.Idle
        };

        // Create workspace directory
        Directory.CreateDirectory(workspace.Path);

        _workspaces.Add(workspace);

        return await Task.FromResult(workspace);
    }

    /// <summary>
    /// Deletes a workspace and its contents
    /// </summary>
    public async Task DeleteWorkspaceAsync(Guid workspaceId)
    {
        var workspace = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (workspace == null)
            return;

        // Delete directory
        if (Directory.Exists(workspace.Path))
        {
            Directory.Delete(workspace.Path, recursive: true);
        }

        _workspaces.Remove(workspace);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets a workspace by ID
    /// </summary>
    public Workspace? GetWorkspace(Guid workspaceId)
    {
        return _workspaces.FirstOrDefault(w => w.Id == workspaceId);
    }

    /// <summary>
    /// Updates workspace last accessed time
    /// </summary>
    public void UpdateLastAccessed(Guid workspaceId)
    {
        var workspace = GetWorkspace(workspaceId);
        if (workspace != null)
        {
            workspace.LastAccessedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Renames a workspace and updates its directory path
    /// </summary>
    public async Task<bool> RenameWorkspaceAsync(Guid workspaceId, string newName)
    {
        var workspace = GetWorkspace(workspaceId);
        if (workspace == null)
            return false;

        if (string.IsNullOrWhiteSpace(newName))
            return false;

        // Check if name already exists
        if (_workspaces.Any(w => w.Id != workspaceId &&
            string.Equals(w.Name, newName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var oldPath = workspace.Path;
        var newPath = Path.Combine(_baseWorkspacePath, SanitizeName(newName));

        // Rename directory if it exists
        if (Directory.Exists(oldPath) && oldPath != newPath)
        {
            try
            {
                Directory.Move(oldPath, newPath);
            }
            catch
            {
                return false;
            }
        }

        // Update workspace properties
        workspace.Name = newName;
        workspace.Path = newPath;

        return await Task.FromResult(true);
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
