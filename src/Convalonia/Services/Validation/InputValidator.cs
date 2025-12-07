using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Convalonia.Services.Validation;

/// <summary>
/// Provides validation methods for user inputs to prevent security vulnerabilities
/// and ensure data integrity.
/// </summary>
public static partial class InputValidator
{
    // Git URL patterns
    private static readonly Regex HttpsGitUrlRegex = GenerateHttpsGitUrlRegex();
    private static readonly Regex SshGitUrlRegex = GenerateSshGitUrlRegex();

    // Branch name validation (Git naming rules)
    private static readonly Regex BranchNameRegex = GenerateBranchNameRegex();

    // Dangerous characters that could be used for injection
    private static readonly char[] DangerousChars = { '`', '$', ';', '|', '&', '>', '<', '\n', '\r' };

    [GeneratedRegex(@"^https://[a-zA-Z0-9\-._~:/?#\[\]@!$&'()*+,;=]+\.git$|^https://github\.com/[\w\-]+/[\w\-\.]+/?$", RegexOptions.Compiled)]
    private static partial Regex GenerateHttpsGitUrlRegex();

    [GeneratedRegex(@"^git@[a-zA-Z0-9\-._]+:[a-zA-Z0-9\-._/]+\.git$", RegexOptions.Compiled)]
    private static partial Regex GenerateSshGitUrlRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9/_\-\.]*[a-zA-Z0-9]$", RegexOptions.Compiled)]
    private static partial Regex GenerateBranchNameRegex();

    /// <summary>
    /// Validates a Git repository URL.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL is valid, false otherwise.</returns>
    public static bool IsValidGitUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Check for HTTPS or SSH format
        return HttpsGitUrlRegex.IsMatch(url) || SshGitUrlRegex.IsMatch(url);
    }

    /// <summary>
    /// Validates a file system path.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="mustExist">Whether the path must exist.</param>
    /// <returns>True if the path is valid, false otherwise.</returns>
    public static bool IsValidPath(string? path, bool mustExist = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check for invalid path characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
                return false;

            // Get full path to normalize and check for path traversal
            var fullPath = Path.GetFullPath(path);

            // If mustExist, verify the path exists
            if (mustExist && !Directory.Exists(fullPath) && !File.Exists(fullPath))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a path is within a specified workspace directory.
    /// This prevents path traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <returns>True if the path is within the workspace, false otherwise.</returns>
    public static bool IsPathInWorkspace(string? path, string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(workspacePath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var workspaceFullPath = Path.GetFullPath(workspacePath);

            // Ensure the path starts with the workspace path
            return fullPath.StartsWith(workspaceFullPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a Git branch name according to Git naming rules.
    /// </summary>
    /// <param name="branchName">The branch name to validate.</param>
    /// <returns>True if the branch name is valid, false otherwise.</returns>
    public static bool IsValidBranchName(string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return false;

        // Check length (Git has a limit, but 255 is reasonable)
        if (branchName.Length > 255)
            return false;

        // Check for invalid patterns
        if (branchName.StartsWith('-') ||
            branchName.EndsWith('.') ||
            branchName.Contains("..") ||
            branchName.Contains("//") ||
            branchName.Contains("@{") ||
            branchName.EndsWith('/') ||
            branchName.EndsWith(".lock"))
            return false;

        // Check regex pattern
        return BranchNameRegex.IsMatch(branchName);
    }

    /// <summary>
    /// Validates a commit message to prevent command injection.
    /// </summary>
    /// <param name="message">The commit message to validate.</param>
    /// <returns>True if the message is safe, false otherwise.</returns>
    public static bool IsValidCommitMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        // Check for dangerous characters that could be used for injection
        if (message.IndexOfAny(DangerousChars) >= 0)
            return false;

        // Check reasonable length (Git typically supports up to 72 chars for subject)
        // But we allow longer messages for body
        if (message.Length > 10000)
            return false;

        return true;
    }

    /// <summary>
    /// Sanitizes a string for safe use in shell commands by escaping special characters.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized string safe for shell use.</returns>
    public static string SanitizeForShell(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Escape quotes and backslashes
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`");
    }

    /// <summary>
    /// Escapes a string for safe use as a Git argument.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>An escaped string safe for Git commands.</returns>
    public static string EscapeGitArgument(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "\"\"";

        // Quote the entire argument and escape internal quotes
        var escaped = input.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
