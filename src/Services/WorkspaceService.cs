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
    private readonly GitHubService _gitHubService;

    public WorkspaceService() : this(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ConvaloniaWorkspaces"),
        new GitHubService())
    {
    }

    public WorkspaceService(string baseWorkspacePath, GitHubService gitHubService)
    {
        _baseWorkspacePath = baseWorkspacePath;
        _gitHubService = gitHubService;

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
    /// <param name="name">Optional workspace name</param>
    /// <param name="gitRemote">Optional git remote URL</param>
    /// <param name="sourceRepoPath">Optional path to local git repository to copy from</param>
    public async Task<Workspace> CreateWorkspaceAsync(string? name = null, string? gitRemote = null, string? sourceRepoPath = null)
    {
        // Generate random name if not provided
        var workspaceName = string.IsNullOrWhiteSpace(name)
            ? RandomNameGenerator.GenerateUnique(_workspaces.Select(w => w.Name))
            : name;

        var workspacePath = Path.Combine(_baseWorkspacePath, SanitizeName(workspaceName));

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = workspaceName,
            Path = workspacePath,
            GitRemote = gitRemote,
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now,
            Status = WorkspaceStatus.Idle
        };

        // Initialize git repository
        bool gitInitialized = false;

        // Priority: Git remote URL > Local repo detection
        if (!string.IsNullOrWhiteSpace(gitRemote))
        {
            // Git remote URL is provided, clone it
            Directory.CreateDirectory(workspacePath);
            gitInitialized = await _gitHubService.CloneRepositoryAsync(gitRemote, workspacePath);

            if (gitInitialized)
            {
                // Create a new branch for this workspace
                var branchName = GitHubService.GenerateBranchName(workspaceName);
                var branchCreated = await _gitHubService.CreateBranchAsync(workspacePath, branchName);

                if (branchCreated)
                {
                    workspace.GitBranch = branchName;
                }
            }
        }
        // No git remote URL, check if source repo path is a git repository
        else if (!string.IsNullOrWhiteSpace(sourceRepoPath) && await _gitHubService.IsGitRepositoryAsync(sourceRepoPath))
        {
            // Get the root of the source repository
            var repoRoot = await _gitHubService.GetRepositoryRootAsync(sourceRepoPath);
            if (repoRoot != null)
            {
                // Copy the repository to workspace directory
                gitInitialized = await _gitHubService.CopyRepositoryAsync(repoRoot, workspacePath);

                if (gitInitialized)
                {
                    // Create a new branch for this workspace
                    var branchName = GitHubService.GenerateBranchName(workspaceName);
                    var branchCreated = await _gitHubService.CreateBranchAsync(workspacePath, branchName);

                    if (branchCreated)
                    {
                        workspace.GitBranch = branchName;
                        workspace.GitRemote = repoRoot; // Store the source repo path
                    }
                }
            }
        }

        // If no git initialization happened, just create an empty directory
        if (!gitInitialized)
        {
            Directory.CreateDirectory(workspacePath);
        }

        _workspaces.Add(workspace);

        return workspace;
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
