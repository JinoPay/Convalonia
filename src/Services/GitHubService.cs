using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Convalonia.Services;

/// <summary>
/// Handles GitHub operations (clone, branch, PR, etc.)
/// </summary>
public class GitHubService
{
    /// <summary>
    /// Clones a GitHub repository to the specified path
    /// </summary>
    public async Task<bool> CloneRepositoryAsync(string repoUrl, string targetPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone {repoUrl} \"{targetPath}\"",
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
            Console.WriteLine($"Failed to clone repository: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a new branch in the repository
    /// </summary>
    public async Task<bool> CreateBranchAsync(string workspacePath, string branchName)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"checkout -b {branchName}",
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
            Console.WriteLine($"Failed to create branch: {ex.Message}");
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
            Console.WriteLine($"Failed to get current branch: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Commits changes with a message
    /// </summary>
    public async Task<bool> CommitChangesAsync(string workspacePath, string message)
    {
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

            // Commit
            var commitInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"commit -m \"{message}\"",
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
            Console.WriteLine($"Failed to commit changes: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pushes changes to remote
    /// </summary>
    public async Task<bool> PushChangesAsync(string workspacePath, string branchName)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"push -u origin {branchName}",
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
            Console.WriteLine($"Failed to push changes: {ex.Message}");
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
            Console.WriteLine($"Failed to check git repository: {ex.Message}");
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
            Console.WriteLine($"Failed to initialize repository: {ex.Message}");
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
            Console.WriteLine($"Failed to get repository root: {ex.Message}");
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
            Console.WriteLine($"Failed to copy repository: {ex.Message}");
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
