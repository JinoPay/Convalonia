using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;
using Microsoft.Extensions.Logging;

namespace Convalonia.Services;

/// <summary>
/// Service for managing checkpoints (snapshots) of workspace state
/// </summary>
public class CheckpointService : ICheckpointService
{
    private readonly IGitService _gitService;
    private readonly ILogger<CheckpointService> _logger;
    private readonly string _checkpointStoragePath;
    private readonly Dictionary<Guid, List<Checkpoint>> _checkpointCache = new();
    private readonly object _lock = new();

    public CheckpointService(IGitService gitService, ILogger<CheckpointService> logger)
    {
        _gitService = gitService;
        _logger = logger;

        _checkpointStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Convalonia",
            "checkpoints"
        );

        Directory.CreateDirectory(_checkpointStoragePath);
    }

    /// <inheritdoc />
    public async Task<Checkpoint> CreateCheckpointAsync(
        Workspace workspace,
        Agent agent,
        int turnNumber,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating checkpoint for agent {AgentId} turn {TurnNumber}", agent.Id, turnNumber);

            // Commit all current changes
            var commitMessage = $"Checkpoint: Turn {turnNumber}";
            await _gitService.CommitAllChangesAsync(workspace.Path, commitMessage, skipHooks: true);

            // Get the commit SHA
            var commitSha = await _gitService.GetCurrentCommitShaAsync(workspace.Path);

            // Create checkpoint object
            var checkpoint = new Checkpoint
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                AgentId = agent.Id,
                TurnNumber = turnNumber,
                GitCommitSha = commitSha,
                CreatedAt = DateTime.Now,
                UserMessage = userMessage,
                AssistantMessage = assistantMessage
            };

            // Store in Git ref
            await _gitService.UpdateRefAsync(workspace.Path, checkpoint.RefName, commitSha);

            // Persist checkpoint metadata to disk
            await SaveCheckpointMetadataAsync(checkpoint, cancellationToken);

            // Update cache
            lock (_lock)
            {
                if (!_checkpointCache.ContainsKey(agent.Id))
                {
                    _checkpointCache[agent.Id] = new List<Checkpoint>();
                }
                _checkpointCache[agent.Id].Add(checkpoint);
            }

            _logger.LogInformation("Created checkpoint {CheckpointId} at SHA {CommitSha}", checkpoint.Id, commitSha);
            return checkpoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint for agent {AgentId} turn {TurnNumber}", agent.Id, turnNumber);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RevertToCheckpointAsync(
        Checkpoint checkpoint,
        Workspace workspace,
        Agent agent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reverting to checkpoint {CheckpointId} (turn {TurnNumber})", checkpoint.Id, checkpoint.TurnNumber);

            // Reset workspace to checkpoint state
            await _gitService.ResetHardAsync(workspace.Path, checkpoint.GitCommitSha);

            // Remove messages after this checkpoint
            var messagesToRemove = agent.Messages
                .Skip(checkpoint.TurnNumber * 2) // Each turn has 2 messages (user + assistant)
                .ToList();

            foreach (var message in messagesToRemove)
            {
                agent.Messages.Remove(message);
            }

            // Delete checkpoints after this point
            var checkpointsToDelete = (await GetCheckpointsAsync(agent.Id, cancellationToken))
                .Where(c => c.TurnNumber > checkpoint.TurnNumber)
                .ToList();

            foreach (var c in checkpointsToDelete)
            {
                await DeleteCheckpointAsync(c, cancellationToken);
            }

            _logger.LogInformation("Reverted to checkpoint {CheckpointId}, removed {MessageCount} messages and {CheckpointCount} checkpoints",
                checkpoint.Id, messagesToRemove.Count, checkpointsToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert to checkpoint {CheckpointId}", checkpoint.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<Checkpoint>> GetCheckpointsAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_checkpointCache.TryGetValue(agentId, out var cached))
            {
                return new List<Checkpoint>(cached);
            }
        }

        // Load from disk if not in cache
        var checkpoints = await LoadCheckpointsFromDiskAsync(agentId, cancellationToken);

        lock (_lock)
        {
            _checkpointCache[agentId] = checkpoints;
        }

        return checkpoints;
    }

    /// <inheritdoc />
    public async Task DeleteCheckpointAsync(Checkpoint checkpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete Git ref
            var workspacePath = Path.Combine(_checkpointStoragePath, "..", "..", "workspaces", checkpoint.WorkspaceId.ToString());
            if (Directory.Exists(workspacePath))
            {
                await _gitService.DeleteRefAsync(workspacePath, checkpoint.RefName);
            }

            // Delete metadata file
            var metadataPath = GetCheckpointMetadataPath(checkpoint.Id);
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            // Remove from cache
            lock (_lock)
            {
                if (_checkpointCache.TryGetValue(checkpoint.AgentId, out var checkpoints))
                {
                    checkpoints.RemoveAll(c => c.Id == checkpoint.Id);
                }
            }

            _logger.LogInformation("Deleted checkpoint {CheckpointId}", checkpoint.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete checkpoint {CheckpointId}", checkpoint.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAllCheckpointsAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var checkpoints = await GetCheckpointsAsync(agentId, cancellationToken);

        foreach (var checkpoint in checkpoints)
        {
            await DeleteCheckpointAsync(checkpoint, cancellationToken);
        }

        lock (_lock)
        {
            _checkpointCache.Remove(agentId);
        }

        _logger.LogInformation("Deleted all checkpoints for agent {AgentId}", agentId);
    }

    private async Task SaveCheckpointMetadataAsync(Checkpoint checkpoint, CancellationToken cancellationToken)
    {
        var filePath = GetCheckpointMetadataPath(checkpoint.Id);
        var json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task<List<Checkpoint>> LoadCheckpointsFromDiskAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var checkpoints = new List<Checkpoint>();

        if (!Directory.Exists(_checkpointStoragePath))
        {
            return checkpoints;
        }

        var files = Directory.GetFiles(_checkpointStoragePath, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var checkpoint = JsonSerializer.Deserialize<Checkpoint>(json);

                if (checkpoint != null && checkpoint.AgentId == agentId)
                {
                    checkpoints.Add(checkpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load checkpoint from {FilePath}", file);
            }
        }

        return checkpoints.OrderBy(c => c.TurnNumber).ToList();
    }

    private string GetCheckpointMetadataPath(Guid checkpointId)
    {
        return Path.Combine(_checkpointStoragePath, $"{checkpointId}.json");
    }
}
