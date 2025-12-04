using System;
using System.Diagnostics;
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
}
