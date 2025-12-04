using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;

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
    /// Creates a new workspace
    /// </summary>
    public async Task<Workspace> CreateWorkspaceAsync(string name, string? gitRemote = null)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = Path.Combine(_baseWorkspacePath, SanitizeName(name)),
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

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
