using System;
using System.Linq;
using FluentValidation;

namespace Convalonia.Validators;

/// <summary>
/// Validates Git commit messages
/// </summary>
public class GitCommitMessageValidator : AbstractValidator<string>
{
    public GitCommitMessageValidator()
    {
        RuleFor(message => message)
            .NotEmpty()
            .WithMessage("Commit message cannot be empty")
            .MaximumLength(1000)
            .WithMessage("Commit message must not exceed 1000 characters")
            .Must(NotContainDangerousCharacters)
            .WithMessage("Commit message contains dangerous characters");
    }

    private bool NotContainDangerousCharacters(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        // Prevent command injection via newline characters and shell metacharacters
        // in git commit messages
        var dangerousPatterns = new[]
        {
            "\n;",      // Newline followed by semicolon
            "\n&",      // Newline followed by ampersand
            "\n|",      // Newline followed by pipe
            "\n$",      // Newline followed by dollar sign
            "\n`",      // Newline followed by backtick
            "$(",       // Command substitution
            "${",       // Variable expansion
            "`"         // Backtick command substitution
        };

        return !dangerousPatterns.Any(pattern => message.Contains(pattern, StringComparison.Ordinal));
    }
}

/// <summary>
/// Request model for validating commit messages
/// </summary>
public record CommitMessageRequest(string Message);

/// <summary>
/// Validator for commit message requests
/// </summary>
public class CommitMessageRequestValidator : AbstractValidator<CommitMessageRequest>
{
    public CommitMessageRequestValidator()
    {
        RuleFor(r => r.Message)
            .SetValidator(new GitCommitMessageValidator());
    }
}
