using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;
using Serilog;

namespace Convalonia.Services;

/// <summary>
/// Persists workspace state to local JSON files
/// </summary>
public class WorkspacePersistenceService : IWorkspacePersistenceService
{
    private readonly string _storageDirectory;
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;

    public WorkspacePersistenceService()
    {
        _logger = Log.ForContext<WorkspacePersistenceService>();

        // Store in AppData/Convalonia/workspaces/
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "Convalonia", "workspaces");
        _settingsFile = Path.Combine(appDataPath, "Convalonia", "settings.json");

        // Create directories if they don't exist
        Directory.CreateDirectory(_storageDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile)!);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _logger.Information("WorkspacePersistenceService initialized. Storage directory: {StorageDirectory}", _storageDirectory);
    }

    public async Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        try
        {
            // Update last accessed timestamp
            workspace.LastAccessedAt = DateTime.UtcNow;

            var filePath = GetWorkspaceFilePath(workspace.Id);

            // Create DTO to avoid serializing ObservableCollections with event handlers
            var dto = new WorkspaceDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Path = workspace.Path,
                GitBranch = workspace.GitBranch,
                GitRemote = workspace.GitRemote,
                CreatedAt = workspace.CreatedAt,
                LastAccessedAt = workspace.LastAccessedAt,
                Status = workspace.Status,
                Agents = workspace.Agents.Select(a => new AgentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    WorkspaceId = a.WorkspaceId,
                    Status = a.Status,
                    Model = a.Model,
                    CreatedAt = a.CreatedAt,
                    CompletedAt = a.CompletedAt
                }).ToList(),
                Repositories = workspace.Repositories.Select(r => new RepositoryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    WorkspacePath = r.WorkspacePath,
                    RootPath = r.RootPath,
                    RemoteOrigin = r.RemoteOrigin,
                    CurrentBranch = r.CurrentBranch,
                    BaseBranch = r.BaseBranch,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.Debug("Workspace saved: {WorkspaceId} -> {FilePath}", workspace.Id, filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save workspace {WorkspaceId}", workspace.Id);
            throw;
        }
    }

    public async Task<Workspace?> LoadWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetWorkspaceFilePath(workspaceId);

            if (!File.Exists(filePath))
            {
                _logger.Debug("Workspace file not found: {WorkspaceId}", workspaceId);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var dto = JsonSerializer.Deserialize<WorkspaceDto>(json, _jsonOptions);

            if (dto == null)
            {
                _logger.Warning("Failed to deserialize workspace: {WorkspaceId}", workspaceId);
                return null;
            }

            var workspace = new Workspace
            {
                Id = dto.Id,
                Name = dto.Name,
                Path = dto.Path,
                GitBranch = dto.GitBranch,
                GitRemote = dto.GitRemote,
                CreatedAt = dto.CreatedAt,
                LastAccessedAt = dto.LastAccessedAt,
                Status = dto.Status
            };

            // Restore agents (messages will be loaded separately)
            foreach (var agentDto in dto.Agents)
            {
                workspace.Agents.Add(new Agent
                {
                    Id = agentDto.Id,
                    Name = agentDto.Name,
                    WorkspaceId = agentDto.WorkspaceId,
                    Status = agentDto.Status,
                    Model = agentDto.Model,
                    CreatedAt = agentDto.CreatedAt,
                    CompletedAt = agentDto.CompletedAt
                });
            }

            // Restore repositories
            foreach (var repoDto in dto.Repositories)
            {
                workspace.Repositories.Add(new Repository
                {
                    Id = repoDto.Id,
                    Name = repoDto.Name,
                    WorkspacePath = repoDto.WorkspacePath,
                    RootPath = repoDto.RootPath,
                    RemoteOrigin = repoDto.RemoteOrigin,
                    CurrentBranch = repoDto.CurrentBranch,
                    BaseBranch = repoDto.BaseBranch,
                    CreatedAt = repoDto.CreatedAt
                });
            }

            _logger.Debug("Workspace loaded: {WorkspaceId} with {AgentCount} agents", workspaceId, workspace.Agents.Count);
            return workspace;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<IEnumerable<Workspace>> LoadAllWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaces = new List<Workspace>();
            var files = Directory.GetFiles(_storageDirectory, "workspace-*.json");

            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var idString = fileName.Replace("workspace-", "");

                    if (Guid.TryParse(idString, out var workspaceId))
                    {
                        var workspace = await LoadWorkspaceAsync(workspaceId, cancellationToken);
                        if (workspace != null)
                        {
                            workspaces.Add(workspace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load workspace from file: {FilePath}", file);
                    // Continue loading other workspaces
                }
            }

            _logger.Information("Loaded {Count} workspaces from storage", workspaces.Count);
            return workspaces.OrderByDescending(w => w.LastAccessedAt);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load workspaces");
            throw;
        }
    }

    public async Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetWorkspaceFilePath(workspaceId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.Debug("Workspace file deleted: {WorkspaceId}", workspaceId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete workspace {WorkspaceId}", workspaceId);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task SaveLastActiveWorkspaceAsync(Guid? workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken);
            settings.LastActiveWorkspaceId = workspaceId;
            await SaveSettingsAsync(settings, cancellationToken);

            _logger.Debug("Last active workspace saved: {WorkspaceId}", workspaceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save last active workspace");
            throw;
        }
    }

    public async Task<Guid?> GetLastActiveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken);
            _logger.Debug("Last active workspace retrieved: {WorkspaceId}", settings.LastActiveWorkspaceId);
            return settings.LastActiveWorkspaceId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get last active workspace");
            return null;
        }
    }

    private string GetWorkspaceFilePath(Guid workspaceId)
    {
        return Path.Combine(_storageDirectory, $"workspace-{workspaceId}.json");
    }

    private async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsFile))
        {
            return new AppSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFile, cancellationToken);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsFile, json, cancellationToken);
    }

    #region DTOs

    private class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? GitBranch { get; set; }
        public string? GitRemote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public WorkspaceStatus Status { get; set; }
        public List<AgentDto> Agents { get; set; } = new();
        public List<RepositoryDto> Repositories { get; set; } = new();
    }

    private class AgentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid WorkspaceId { get; set; }
        public AgentStatus Status { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    private class RepositoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WorkspacePath { get; set; } = string.Empty;
        public string? RootPath { get; set; }
        public string? RemoteOrigin { get; set; }
        public string? CurrentBranch { get; set; }
        public string? BaseBranch { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class AppSettings
    {
        public Guid? LastActiveWorkspaceId { get; set; }
        public Dictionary<Guid, Guid?> LastActiveAgents { get; set; } = new();
    }

    #endregion
}
