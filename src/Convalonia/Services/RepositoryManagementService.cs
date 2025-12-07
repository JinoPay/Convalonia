using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;
using Serilog;

namespace Convalonia.Services;

/// <summary>
/// Manages source repositories (the top-level git repositories)
/// Each source repository can have multiple workspaces
/// </summary>
public class RepositoryManagementService : IRepositoryManagementService
{
    private readonly ILogger _logger = Log.ForContext<RepositoryManagementService>();
    private readonly ObservableCollection<SourceRepository> _repositories = new();
    private readonly string _baseDataPath;
    private readonly IGitService _gitHubService;
    private readonly IWorkspaceService _workspaceService;
    private bool _isInitialized;

    public RepositoryManagementService(
        IGitService gitHubService,
        IWorkspaceService workspaceService)
    {
        _gitHubService = gitHubService;
        _workspaceService = workspaceService;
        _baseDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ConvaloniaRepositories");

        // Create base directory if it doesn't exist
        if (!Directory.Exists(_baseDataPath))
        {
            Directory.CreateDirectory(_baseDataPath);
        }

        // Note: InitializeAsync() must be called after construction to load repositories
        // This avoids the deadlock issue from calling .Wait() in constructor
    }

    public ObservableCollection<SourceRepository> Repositories => _repositories;

    /// <summary>
    /// Initializes the service by loading repositories asynchronously.
    /// Must be called once after construction before using other methods.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await LoadRepositoriesAsync();
        _isInitialized = true;
    }

    /// <summary>
    /// Adds a local git repository
    /// </summary>
    public async Task<SourceRepository> AddLocalRepositoryAsync(string localPath)
    {
        // Validate it's a git repository
        if (!await _gitHubService.IsGitRepositoryAsync(localPath))
        {
            throw new InvalidOperationException("The specified path is not a git repository");
        }

        // Get repository root
        var repoRoot = await _gitHubService.GetRepositoryRootAsync(localPath);
        if (string.IsNullOrEmpty(repoRoot))
        {
            throw new InvalidOperationException("Could not determine repository root");
        }

        // Check if already added
        if (_repositories.Any(r => r.SourcePath == repoRoot))
        {
            throw new InvalidOperationException("This repository is already added");
        }

        var repoName = Path.GetFileName(repoRoot);
        var repository = new SourceRepository
        {
            Id = Guid.NewGuid(),
            Name = repoName,
            SourcePath = repoRoot,
            SourceType = RepositorySourceType.Local,
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now
        };

        _repositories.Add(repository);
        await SaveRepositoriesAsync();

        return repository;
    }

    /// <summary>
    /// Adds a remote git repository (clones it first)
    /// </summary>
    public async Task<SourceRepository> AddRemoteRepositoryAsync(string gitUrl)
    {
        // Validate git URL
        if (!await _gitHubService.ValidateGitUrlAsync(gitUrl))
        {
            throw new InvalidOperationException("Invalid or inaccessible git URL");
        }

        // Check if already added
        if (_repositories.Any(r => r.SourcePath == gitUrl))
        {
            throw new InvalidOperationException("This repository is already added");
        }

        // Extract repository name from URL
        var repoName = ExtractRepoNameFromUrl(gitUrl);
        var clonePath = Path.Combine(_baseDataPath, repoName);

        // Clone repository
        if (!await _gitHubService.CloneRepositoryAsync(gitUrl, clonePath))
        {
            throw new InvalidOperationException("Failed to clone repository");
        }

        var repository = new SourceRepository
        {
            Id = Guid.NewGuid(),
            Name = repoName,
            SourcePath = clonePath, // Use local clone path, not git URL
            SourceType = RepositorySourceType.Remote,
            RemoteUrl = gitUrl, // Store the original URL separately
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now
        };

        _repositories.Add(repository);
        await SaveRepositoriesAsync();

        return repository;
    }

    /// <summary>
    /// Creates a new repository by initializing git in a folder
    /// </summary>
    public async Task<SourceRepository> CreateNewRepositoryAsync(string folderPath)
    {
        // Initialize git repository
        if (!await _gitHubService.InitRepositoryAsync(folderPath))
        {
            throw new InvalidOperationException("Failed to initialize git repository");
        }

        var repoName = Path.GetFileName(folderPath);
        var repository = new SourceRepository
        {
            Id = Guid.NewGuid(),
            Name = repoName,
            SourcePath = folderPath,
            SourceType = RepositorySourceType.Local,
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now
        };

        _repositories.Add(repository);
        await SaveRepositoriesAsync();

        return repository;
    }

    /// <summary>
    /// Removes a repository
    /// </summary>
    public async Task RemoveRepositoryAsync(Guid repositoryId)
    {
        var repository = _repositories.FirstOrDefault(r => r.Id == repositoryId);
        if (repository == null) return;

        // Delete all workspaces for this repository
        foreach (var workspace in repository.Workspaces.ToList())
        {
            await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
        }

        _repositories.Remove(repository);
        await SaveRepositoriesAsync();
    }

    /// <summary>
    /// Updates the last accessed timestamp
    /// </summary>
    public async Task UpdateLastAccessedAsync(Guid repositoryId)
    {
        var repository = _repositories.FirstOrDefault(r => r.Id == repositoryId);
        if (repository != null)
        {
            repository.LastAccessedAt = DateTime.Now;
            await SaveRepositoriesAsync();
        }
    }

    /// <summary>
    /// Creates a workspace for a repository
    /// </summary>
    public async Task<Workspace> CreateWorkspaceAsync(SourceRepository repository, string? workspaceName = null)
    {
        var sourcePath = repository.SourceType == RepositorySourceType.Remote
            ? Path.Combine(_baseDataPath, repository.Name)
            : repository.SourcePath;

        var workspace = await _workspaceService.CreateWorkspaceAsync(
            name: workspaceName,
            gitRemote: null,
            sourceRepoPath: sourcePath);

        repository.Workspaces.Add(workspace);
        repository.LastAccessedAt = DateTime.Now;
        await SaveRepositoriesAsync();

        return workspace;
    }

    private string ExtractRepoNameFromUrl(string gitUrl)
    {
        // Extract repository name from URLs like:
        // https://github.com/user/repo.git -> repo
        // git@github.com:user/repo.git -> repo
        var parts = gitUrl.TrimEnd('/').Split('/');
        var lastPart = parts[^1];
        return lastPart.EndsWith(".git")
            ? lastPart.Substring(0, lastPart.Length - 4)
            : lastPart;
    }

    private async Task LoadRepositoriesAsync()
    {
        try
        {
            var configPath = Path.Combine(_baseDataPath, "repositories.json");
            if (!File.Exists(configPath))
                return;

            var json = await File.ReadAllTextAsync(configPath);
            var repositories = System.Text.Json.JsonSerializer.Deserialize<List<SourceRepository>>(json);

            if (repositories != null)
            {
                _repositories.Clear();
                foreach (var repo in repositories)
                {
                    _repositories.Add(repo);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            _logger.Error(ex, "Failed to load repositories from {BasePath}", _baseDataPath);
        }
    }

    private async Task SaveRepositoriesAsync()
    {
        try
        {
            var configPath = Path.Combine(_baseDataPath, "repositories.json");
            var json = System.Text.Json.JsonSerializer.Serialize(_repositories.ToList(), new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(configPath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            _logger.Error(ex, "Failed to save repositories to {BasePath}", _baseDataPath);
        }
    }
}
