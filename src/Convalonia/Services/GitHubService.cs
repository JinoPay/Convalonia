using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Models;
using Convalonia.Services.Validation;
using Convalonia.Validators;
using Serilog;

namespace Convalonia.Services;

/// <summary>
/// Handles GitHub operations (clone, branch, PR, etc.)
/// </summary>
public class GitHubService : IGitService
{
    private readonly ILogger _logger = Log.ForContext<GitHubService>();
    /// <summary>
    /// Validates if a Git URL is accessible
    /// </summary>
    public async Task<bool> ValidateGitUrlAsync(string repoUrl)
    {
        // Validate Git URL format
        if (!InputValidator.IsValidGitUrl(repoUrl))
            throw new ValidationException("repoUrl", "Invalid Git repository URL");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"ls-remote {InputValidator.EscapeGitArgument(repoUrl)} HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate git URL: {RepoUrl}", repoUrl);
            return false;
        }
    }

    /// <summary>
    /// Clones a GitHub repository to the specified path
    /// </summary>
    public async Task<bool> CloneRepositoryAsync(string repoUrl, string targetPath)
    {
        // Validate inputs
        if (!InputValidator.IsValidGitUrl(repoUrl))
            throw new ValidationException("repoUrl", "Invalid Git repository URL");
        if (!InputValidator.IsValidPath(targetPath))
            throw new ValidationException("targetPath", "Invalid target path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone {InputValidator.EscapeGitArgument(repoUrl)} {InputValidator.EscapeGitArgument(targetPath)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clone repository from {RepoUrl} to {TargetPath}", repoUrl, targetPath);
            return false;
        }
    }

    /// <summary>
    /// Creates a new branch in the repository
    /// </summary>
    public async Task<bool> CreateBranchAsync(string workspacePath, string branchName, string? baseBranch = null)
    {
        // Validate inputs
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (!InputValidator.IsValidBranchName(branchName))
            throw new ValidationException("branchName", "Invalid branch name");
        if (!string.IsNullOrWhiteSpace(baseBranch) && !InputValidator.IsValidBranchName(baseBranch))
            throw new ValidationException("baseBranch", "Invalid base branch name");

        try
        {
            // If base branch is provided, ensure we're starting from it
            if (!string.IsNullOrWhiteSpace(baseBranch))
            {
                // Fetch latest changes first
                await ExecuteGitCommandAsync(workspacePath, "fetch --all");

                // Check if base branch exists locally or remotely
                var branchExists = await ExecuteGitCommandAsync(workspacePath, $"rev-parse --verify {baseBranch}");
                if (!branchExists)
                {
                    // Try remote branch
                    branchExists = await ExecuteGitCommandAsync(workspacePath, $"rev-parse --verify origin/{baseBranch}");
                    if (branchExists)
                    {
                        // Checkout remote branch first
                        await ExecuteGitCommandAsync(workspacePath, $"checkout -b {baseBranch} origin/{baseBranch}");
                    }
                }
            }

            var arguments = string.IsNullOrWhiteSpace(baseBranch)
                ? $"checkout -b {branchName}"
                : $"checkout -b {branchName} {baseBranch}";

            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create branch {BranchName} in {WorkspacePath}", branchName, workspacePath);
            return false;
        }
    }

    /// <summary>
    /// Gets the current branch name
    /// </summary>
    public async Task<string?> GetCurrentBranchAsync(string workspacePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "branch --show-current",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get current branch in {WorkspacePath}", workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Commits changes with a message
    /// </summary>
    public async Task<bool> CommitChangesAsync(string workspacePath, string message)
    {
        // Validate inputs
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (!InputValidator.IsValidCommitMessage(message))
            throw new CommandInjectionException(message);

        try
        {
            // Stage all changes
            var stageInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "add -A",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var stageProcess = Process.Start(stageInfo))
            {
                if (stageProcess == null)
                    return false;
                await stageProcess.WaitForExitAsync();
                if (stageProcess.ExitCode != 0)
                    return false;
            }

            // Commit - Use EscapeGitArgument to prevent command injection
            var commitInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"commit -m {InputValidator.EscapeGitArgument(message)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var commitProcess = Process.Start(commitInfo);
            if (commitProcess == null)
                return false;

            await commitProcess.WaitForExitAsync();
            return commitProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to commit changes in {WorkspacePath} with message: {Message}", workspacePath, message);
            return false;
        }
    }

    /// <summary>
    /// Pushes changes to remote
    /// </summary>
    public async Task<bool> PushChangesAsync(string workspacePath, string branchName)
    {
        // Validate inputs
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (!InputValidator.IsValidBranchName(branchName))
            throw new ValidationException("branchName", "Invalid branch name");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"push -u origin {InputValidator.EscapeGitArgument(branchName)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to push changes in {WorkspacePath} for branch {BranchName}", workspacePath, branchName);
            return false;
        }
    }

    /// <summary>
    /// Checks if a directory is a git repository
    /// </summary>
    public async Task<bool> IsGitRepositoryAsync(string path)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --git-dir",
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check if {Path} is a git repository", path);
            return false;
        }
    }

    /// <summary>
    /// Initializes a git repository in the specified directory
    /// </summary>
    public async Task<bool> InitRepositoryAsync(string workspacePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize repository at {Path}", workspacePath);
            return false;
        }
    }

    /// <summary>
    /// Gets the root directory of the git repository
    /// </summary>
    public async Task<string?> GetRepositoryRootAsync(string path)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get repository root for {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Copies a local git repository to a new location while preserving git history
    /// </summary>
    public async Task<bool> CopyRepositoryAsync(string sourceRepoPath, string targetPath)
    {
        try
        {
            // Clone the local repository (preserves all git history and branches)
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone \"{sourceRepoPath}\" \"{targetPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to copy repository from {SourcePath} to {TargetPath}", sourceRepoPath, targetPath);
            return false;
        }
    }

    /// <summary>
    /// Checks out a specific branch
    /// </summary>
    public async Task<bool> CheckoutBranchAsync(string workspacePath, string branchName)
    {
        // Validate inputs
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (!InputValidator.IsValidBranchName(branchName))
            throw new ValidationException("branchName", "Invalid branch name");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"checkout {InputValidator.EscapeGitArgument(branchName)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to checkout branch {BranchName} in {WorkspacePath}", branchName, workspacePath);
            return false;
        }
    }

    /// <summary>
    /// Gets all branches in the repository
    /// </summary>
    public async Task<string[]> GetBranchesAsync(string workspacePath, bool includeArchived = false)
    {
        try
        {
            // Get local and remote branches
            var arguments = includeArchived ? "branch -a" : "branch";

            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return Array.Empty<string>();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return Array.Empty<string>();

            // Parse branch output
            var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim().TrimStart('*').Trim())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray();

            return branches;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get branches in {WorkspacePath}", workspacePath);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the remote origin URL
    /// </summary>
    public async Task<string?> GetRemoteOriginAsync(string workspacePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "remote get-url origin",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get remote origin in {WorkspacePath}", workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Checks if repository has uncommitted changes
    /// </summary>
    public async Task<bool> HasUncommittedChangesAsync(string workspacePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status --porcelain",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check for uncommitted changes in {WorkspacePath}", workspacePath);
            return false;
        }
    }

    /// <summary>
    /// Gets the last commit hash
    /// </summary>
    public async Task<string?> GetLastCommitHashAsync(string workspacePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get last commit hash in {WorkspacePath}", workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Helper method to execute git commands
    /// </summary>
    private async Task<bool> ExecuteGitCommandAsync(string workspacePath, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a sanitized branch name from workspace name
    /// </summary>
    public static string GenerateBranchName(string workspaceName)
    {
        // Convert to lowercase and replace spaces/special chars with hyphens
        var branchName = workspaceName
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any characters that aren't alphanumeric or hyphens
        branchName = new string(branchName
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());

        // Remove consecutive hyphens
        while (branchName.Contains("--"))
        {
            branchName = branchName.Replace("--", "-");
        }

        // Trim hyphens from start and end
        branchName = branchName.Trim('-');

        // If empty after sanitization, use a default
        if (string.IsNullOrEmpty(branchName))
        {
            branchName = "workspace";
        }

        return branchName;
    }

    #region Checkpoint Operations

    /// <summary>
    /// Gets the current commit SHA
    /// </summary>
    public async Task<string> GetCurrentCommitShaAsync(string workspacePath)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start git process");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new InvalidOperationException("Failed to get current commit SHA");

            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get current commit SHA in {WorkspacePath}", workspacePath);
            throw;
        }
    }

    /// <summary>
    /// Updates a Git ref to point to a specific commit
    /// </summary>
    public async Task UpdateRefAsync(string workspacePath, string refName, string commitSha)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"update-ref {InputValidator.EscapeGitArgument(refName)} {InputValidator.EscapeGitArgument(commitSha)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start git process");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to update ref: {error}");
            }

            _logger.Information("Updated ref {RefName} to {CommitSha} in {WorkspacePath}", refName, commitSha, workspacePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update ref {RefName} in {WorkspacePath}", refName, workspacePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the commit SHA that a ref points to
    /// </summary>
    public async Task<string?> GetRefAsync(string workspacePath, string refName)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"rev-parse {InputValidator.EscapeGitArgument(refName)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return null;

            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get ref {RefName} in {WorkspacePath}", refName, workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Deletes a Git ref
    /// </summary>
    public async Task DeleteRefAsync(string workspacePath, string refName)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"update-ref -d {InputValidator.EscapeGitArgument(refName)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start git process");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to delete ref: {error}");
            }

            _logger.Information("Deleted ref {RefName} in {WorkspacePath}", refName, workspacePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete ref {RefName} in {WorkspacePath}", refName, workspacePath);
            throw;
        }
    }

    /// <summary>
    /// Performs a hard reset to a specific commit
    /// </summary>
    public async Task ResetHardAsync(string workspacePath, string commitSha)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"reset --hard {InputValidator.EscapeGitArgument(commitSha)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start git process");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to reset: {error}");
            }

            _logger.Information("Reset to {CommitSha} in {WorkspacePath}", commitSha, workspacePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to reset to {CommitSha} in {WorkspacePath}", commitSha, workspacePath);
            throw;
        }
    }

    /// <summary>
    /// Commits all changes (including untracked files) with optional hook skipping
    /// </summary>
    public async Task<bool> CommitAllChangesAsync(string workspacePath, string message, bool skipHooks = false)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        // Validate commit message for command injection
        var validator = new CommitMessageRequestValidator();
        var validationResult = await validator.ValidateAsync(new CommitMessageRequest(message));
        if (!validationResult.IsValid)
            throw new ValidationException("message", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        try
        {
            // First, add all changes
            var addProcessInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "add -A",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var addProcess = Process.Start(addProcessInfo))
            {
                if (addProcess == null)
                    return false;

                await addProcess.WaitForExitAsync();
                if (addProcess.ExitCode != 0)
                    return false;
            }

            // Then commit with optional --no-verify
            var commitArgs = skipHooks
                ? $"commit --no-verify -m {InputValidator.EscapeGitArgument(message)}"
                : $"commit -m {InputValidator.EscapeGitArgument(message)}";

            var commitProcessInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = commitArgs,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var commitProcess = Process.Start(commitProcessInfo);
            if (commitProcess == null)
                return false;

            await commitProcess.WaitForExitAsync();

            if (commitProcess.ExitCode == 0)
            {
                _logger.Information("Committed all changes in {WorkspacePath}", workspacePath);
                return true;
            }

            // Exit code 1 might mean "nothing to commit"
            var output = await commitProcess.StandardOutput.ReadToEndAsync();
            if (output.Contains("nothing to commit"))
            {
                _logger.Debug("Nothing to commit in {WorkspacePath}", workspacePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to commit all changes in {WorkspacePath}", workspacePath);
            return false;
        }
    }

    #endregion

    #region Diff Operations

    /// <summary>
    /// Gets the diff for the workspace
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <param name="compareSpec">Optional compare spec (e.g., "main...HEAD", "HEAD~1", etc.)</param>
    /// <returns>Diff output</returns>
    public async Task<string> GetDiffAsync(string workspacePath, string? compareSpec = null)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var arguments = string.IsNullOrWhiteSpace(compareSpec)
                ? "diff HEAD"
                : $"diff {InputValidator.EscapeGitArgument(compareSpec)}";

            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return string.Empty;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get diff in {WorkspacePath}", workspacePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets list of changed files in the workspace
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <param name="includeUntracked">Whether to include untracked files</param>
    /// <returns>Array of file paths</returns>
    public async Task<string[]> GetChangedFilesAsync(string workspacePath, bool includeUntracked = true)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var arguments = includeUntracked
                ? "status --porcelain"
                : "status --porcelain --untracked-files=no";

            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return Array.Empty<string>();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return Array.Empty<string>();

            // Parse git status output (format: "XY filename")
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Length > 3 ? line[3..].Trim() : string.Empty)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get changed files in {WorkspacePath}", workspacePath);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets diff for a specific file
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <param name="filePath">Relative file path</param>
    /// <returns>File diff output</returns>
    public async Task<string> GetFileDiffAsync(string workspacePath, string filePath)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ValidationException("filePath", "File path cannot be empty");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"diff HEAD -- {InputValidator.EscapeGitArgument(filePath)}",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return string.Empty;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get diff for file {FilePath} in {WorkspacePath}", filePath, workspacePath);
            return string.Empty;
        }
    }

    #endregion

    #region Pull Request Operations

    /// <summary>
    /// Pushes the current branch to remote
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <param name="branchName">Branch name to push</param>
    /// <param name="setUpstream">Whether to set upstream (-u flag)</param>
    /// <returns>True if push succeeded</returns>
    public async Task<bool> PushBranchAsync(string workspacePath, string branchName, bool setUpstream = true)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (!InputValidator.IsValidBranchName(branchName))
            throw new ValidationException("branchName", "Invalid branch name");

        try
        {
            var arguments = setUpstream
                ? $"push -u origin {InputValidator.EscapeGitArgument(branchName)}"
                : $"push origin {InputValidator.EscapeGitArgument(branchName)}";

            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.Warning("Failed to push branch {BranchName}: {Error}", branchName, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to push branch {BranchName} in {WorkspacePath}", branchName, workspacePath);
            return false;
        }
    }

    /// <summary>
    /// Creates a pull request using GitHub CLI (gh)
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <param name="title">PR title</param>
    /// <param name="body">PR body/description</param>
    /// <param name="baseBranch">Base branch (default: main)</param>
    /// <returns>PR URL if successful, null otherwise</returns>
    public async Task<string?> CreatePullRequestAsync(string workspacePath, string title, string body, string baseBranch = "main")
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("title", "PR title cannot be empty");
        if (!InputValidator.IsValidBranchName(baseBranch))
            throw new ValidationException("baseBranch", "Invalid base branch name");

        try
        {
            // Build gh pr create command
            var arguments = $"pr create --base {InputValidator.EscapeGitArgument(baseBranch)} " +
                          $"--title {InputValidator.EscapeGitArgument(title)} " +
                          $"--body {InputValidator.EscapeGitArgument(body)}";

            var processInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = arguments,
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.Error("Failed to start gh process");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.Error("Failed to create PR: {Error}", error);
                return null;
            }

            // Extract PR URL from output (gh outputs the URL)
            var prUrl = output.Trim().Split('\n').LastOrDefault()?.Trim();
            _logger.Information("Created PR: {PrUrl}", prUrl);

            return prUrl;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create pull request in {WorkspacePath}", workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Gets the current remote URL
    /// </summary>
    /// <param name="workspacePath">Workspace path</param>
    /// <returns>Remote URL or null</returns>
    public async Task<string?> GetCurrentRemoteUrlAsync(string workspacePath)
    {
        if (!InputValidator.IsValidPath(workspacePath))
            throw new ValidationException("workspacePath", "Invalid workspace path");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "remote get-url origin",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get remote URL in {WorkspacePath}", workspacePath);
            return null;
        }
    }

    /// <summary>
    /// Checks if GitHub CLI (gh) is installed
    /// </summary>
    public async Task<bool> IsGitHubCliInstalledAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "GitHub CLI is not installed or not accessible");
            return false;
        }
    }

    #endregion
}
