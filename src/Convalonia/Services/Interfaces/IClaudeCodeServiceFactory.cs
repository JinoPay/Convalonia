namespace Convalonia.Services;

/// <summary>
/// Factory for creating ClaudeCodeService instances with workspace paths
/// </summary>
public interface IClaudeCodeServiceFactory
{
    /// <summary>
    /// Creates a new ClaudeCodeService instance for the specified workspace path
    /// </summary>
    /// <param name="workspacePath">The workspace directory path</param>
    /// <returns>A new ClaudeCodeService instance</returns>
    ClaudeCodeService Create(string workspacePath);
}
