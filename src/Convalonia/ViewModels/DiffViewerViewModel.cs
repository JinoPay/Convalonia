using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services;
using Convalonia.Services.Toast;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for displaying Git diffs
/// </summary>
public partial class DiffViewerViewModel : ReactiveObject
{
    private readonly IGitService _gitService;
    private readonly IToastService _toastService;
    private readonly ILogger _logger = Log.ForContext<DiffViewerViewModel>();

    [Reactive]
    private ObservableCollection<FileDiff> _changedFiles = new();

    [Reactive]
    private FileDiff? _selectedFile;

    [Reactive]
    private Workspace? _workspace;

    [Reactive]
    private bool _isLoading;

    [Reactive]
    private string _diffSummary = string.Empty;

    [ReactiveCommand]
    private async Task LoadDiffsAsync()
    {
        if (Workspace == null)
        {
            _toastService.ShowWarning("No workspace selected");
            return;
        }

        try
        {
            IsLoading = true;
            ChangedFiles.Clear();

            // Get changed files list
            var changedFilesList = await _gitService.GetChangedFilesAsync(Workspace.Path, includeUntracked: true);

            if (changedFilesList.Length == 0)
            {
                DiffSummary = "No changes to display";
                _toastService.ShowInfo("No changes found");
                return;
            }

            // Get full diff output
            var diffOutput = await _gitService.GetDiffAsync(Workspace.Path);

            // Parse diff
            var fileDiffs = DiffParser.Parse(diffOutput);

            // Get status for change types
            var statusOutput = await GetStatusOutputAsync(Workspace.Path);
            var statusInfo = DiffParser.ParseStatus(statusOutput);

            // Merge status info with parsed diffs
            foreach (var (filePath, changeType) in statusInfo)
            {
                var existingDiff = fileDiffs.FirstOrDefault(d => d.FilePath == filePath);
                if (existingDiff != null)
                {
                    // Update with status info
                    var index = fileDiffs.IndexOf(existingDiff);
                    fileDiffs[index] = existingDiff with { ChangeType = changeType };
                }
                else
                {
                    // Add files without diff hunks (e.g., new untracked files)
                    fileDiffs.Add(new FileDiff
                    {
                        FilePath = filePath,
                        ChangeType = changeType,
                        AddedLines = 0,
                        DeletedLines = 0,
                        Hunks = new()
                    });
                }
            }

            // Sort by change type and file path
            var sortedDiffs = fileDiffs
                .OrderBy(d => d.ChangeType)
                .ThenBy(d => d.FilePath)
                .ToList();

            foreach (var diff in sortedDiffs)
            {
                ChangedFiles.Add(diff);
            }

            // Update summary
            var totalAdded = fileDiffs.Sum(d => d.AddedLines);
            var totalDeleted = fileDiffs.Sum(d => d.DeletedLines);
            DiffSummary = $"{fileDiffs.Count} file(s) changed: +{totalAdded} -{totalDeleted}";

            _logger.Information("Loaded diffs for workspace {WorkspaceName}: {FileCount} files",
                Workspace.Name, fileDiffs.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load diffs for workspace {WorkspacePath}", Workspace?.Path);
            _toastService.ShowError($"Failed to load diffs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ReactiveCommand]
    public async Task RefreshAsync()
    {
        await LoadDiffsAsync();
    }

    public DiffViewerViewModel(
        IGitService gitService,
        IToastService toastService)
    {
        _gitService = gitService;
        _toastService = toastService;
    }

    /// <summary>
    /// Sets the workspace and loads its diffs
    /// </summary>
    public async Task SetWorkspaceAsync(Workspace workspace)
    {
        Workspace = workspace;
        await LoadDiffsAsync();
    }

    /// <summary>
    /// Gets raw git status output
    /// </summary>
    private async Task<string> GetStatusOutputAsync(string workspacePath)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status --porcelain",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
                return string.Empty;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
