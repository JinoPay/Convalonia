using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Convalonia.Services.Validation;
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
}
