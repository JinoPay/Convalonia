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
/// Persists agent conversation history to local JSON files
/// </summary>
public class AgentPersistenceService : IAgentPersistenceService
{
    private readonly string _storageDirectory;
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;

    public AgentPersistenceService()
    {
        _logger = Log.ForContext<AgentPersistenceService>();

        // Store in AppData/Convalonia/agents/
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "Convalonia", "agents");
        _settingsFile = Path.Combine(appDataPath, "Convalonia", "settings.json");

        // Create directories if they don't exist
        Directory.CreateDirectory(_storageDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _logger.Information("AgentPersistenceService initialized. Storage directory: {StorageDirectory}", _storageDirectory);
    }

    public async Task SaveAgentMessagesAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetAgentMessagesFilePath(agent.Id);

            // Create DTO to avoid serializing ObservableCollections
            var dto = new AgentMessagesDto
            {
                AgentId = agent.Id,
                WorkspaceId = agent.WorkspaceId,
                Messages = agent.Messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    ToolName = m.ToolName,
                    ToolInput = m.ToolInput,
                    ToolOutput = m.ToolOutput,
                    TurnNumber = m.TurnNumber
                }).ToList(),
                SavedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.Debug("Agent messages saved: {AgentId}, {MessageCount} messages", agent.Id, agent.Messages.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save agent messages {AgentId}", agent.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> LoadAgentMessagesAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetAgentMessagesFilePath(agentId);

            if (!File.Exists(filePath))
            {
                _logger.Debug("Agent messages file not found: {AgentId}", agentId);
                return Enumerable.Empty<Message>();
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var dto = JsonSerializer.Deserialize<AgentMessagesDto>(json, _jsonOptions);

            if (dto == null)
            {
                _logger.Warning("Failed to deserialize agent messages: {AgentId}", agentId);
                return Enumerable.Empty<Message>();
            }

            var messages = dto.Messages.Select(m => new Message
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                ToolName = m.ToolName,
                ToolInput = m.ToolInput,
                ToolOutput = m.ToolOutput,
                TurnNumber = m.TurnNumber
            }).ToList();

            _logger.Debug("Agent messages loaded: {AgentId}, {MessageCount} messages", agentId, messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load agent messages {AgentId}", agentId);
            throw;
        }
    }

    public async Task DeleteAgentMessagesAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetAgentMessagesFilePath(agentId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.Debug("Agent messages file deleted: {AgentId}", agentId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete agent messages {AgentId}", agentId);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task SaveLastActiveAgentAsync(Guid workspaceId, Guid? agentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken);
            settings.LastActiveAgents[workspaceId] = agentId;
            await SaveSettingsAsync(settings, cancellationToken);

            _logger.Debug("Last active agent saved for workspace {WorkspaceId}: {AgentId}", workspaceId, agentId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save last active agent for workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<Guid?> GetLastActiveAgentAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await LoadSettingsAsync(cancellationToken);
            settings.LastActiveAgents.TryGetValue(workspaceId, out var agentId);
            _logger.Debug("Last active agent retrieved for workspace {WorkspaceId}: {AgentId}", workspaceId, agentId);
            return agentId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get last active agent for workspace {WorkspaceId}", workspaceId);
            return null;
        }
    }

    private string GetAgentMessagesFilePath(Guid agentId)
    {
        return Path.Combine(_storageDirectory, $"agent-{agentId}-messages.json");
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

    private class AgentMessagesDto
    {
        public Guid AgentId { get; set; }
        public Guid WorkspaceId { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
        public DateTime SavedAt { get; set; }
    }

    private class MessageDto
    {
        public Guid Id { get; set; }
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? ToolName { get; set; }
        public string? ToolInput { get; set; }
        public string? ToolOutput { get; set; }
        public int TurnNumber { get; set; }
    }

    private class AppSettings
    {
        public Guid? LastActiveWorkspaceId { get; set; }
        public Dictionary<Guid, Guid?> LastActiveAgents { get; set; } = new();
    }

    #endregion
}
