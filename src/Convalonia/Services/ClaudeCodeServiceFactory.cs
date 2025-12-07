namespace Convalonia.Services;

/// <summary>
/// Factory implementation for creating ClaudeCodeService instances
/// </summary>
public class ClaudeCodeServiceFactory : IClaudeCodeServiceFactory
{
    /// <summary>
    /// Creates a new ClaudeCodeService instance for the specified workspace path
    /// </summary>
    /// <param name="workspacePath">The workspace directory path</param>
    /// <returns>A new ClaudeCodeService instance</returns>
    public ClaudeCodeService Create(string workspacePath)
    {
        return new ClaudeCodeService(workspacePath);
    }
}
