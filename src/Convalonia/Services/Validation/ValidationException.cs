using System;

namespace Convalonia.Services.Validation;

/// <summary>
/// Exception thrown when input validation fails.
/// </summary>
public class ValidationException : Exception
{
    public string? FieldName { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when path traversal attack is detected.
/// </summary>
public class PathTraversalException : ValidationException
{
    public string AttemptedPath { get; }
    public string WorkspacePath { get; }

    public PathTraversalException(string attemptedPath, string workspacePath)
        : base($"Path traversal detected: '{attemptedPath}' is outside workspace '{workspacePath}'")
    {
        AttemptedPath = attemptedPath;
        WorkspacePath = workspacePath;
    }
}

/// <summary>
/// Exception thrown when command injection attempt is detected.
/// </summary>
public class CommandInjectionException : ValidationException
{
    public string AttemptedInput { get; }

    public CommandInjectionException(string attemptedInput)
        : base($"Potential command injection detected in input")
    {
        AttemptedInput = attemptedInput;
    }
}
