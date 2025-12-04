using System;

namespace Convalonia.Models;

/// <summary>
/// Represents a message in the conversation between user and Claude
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ToolName { get; set; }
    public string? ToolInput { get; set; }
    public string? ToolOutput { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool
}
