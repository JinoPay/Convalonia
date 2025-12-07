using System;
using System.IO;
using FluentValidation;
using Convalonia.Models;

namespace Convalonia.Validators;

/// <summary>
/// Validates Workspace model properties
/// </summary>
public class WorkspaceValidator : AbstractValidator<Workspace>
{
    public WorkspaceValidator()
    {
        RuleFor(w => w.Id)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty");

        RuleFor(w => w.Name)
            .NotEmpty()
            .WithMessage("Workspace name is required")
            .MaximumLength(255)
            .WithMessage("Workspace name must not exceed 255 characters")
            .Matches(@"^[a-zA-Z0-9_\-\. ]+$")
            .WithMessage("Workspace name can only contain alphanumeric characters, spaces, hyphens, underscores, and dots");

        RuleFor(w => w.Path)
            .NotEmpty()
            .WithMessage("Workspace path is required")
            .Must(BeValidPath)
            .WithMessage("Workspace path must be a valid absolute path");

        RuleFor(w => w.GitBranch)
            .Must(BeValidBranchName!)
            .When(w => !string.IsNullOrEmpty(w.GitBranch))
            .WithMessage("Invalid Git branch name");

        RuleFor(w => w.GitRemote)
            .Must(BeValidGitUrl!)
            .When(w => !string.IsNullOrEmpty(w.GitRemote))
            .WithMessage("Invalid Git URL format");

        RuleFor(w => w.CreatedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Created date cannot be in the future");

        RuleFor(w => w.LastAccessedAt)
            .GreaterThanOrEqualTo(w => w.CreatedAt)
            .WithMessage("Last accessed date must be after or equal to created date");
    }

    private bool BeValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            return Path.IsPathRooted(fullPath);
        }
        catch
        {
            return false;
        }
    }

    private bool BeValidBranchName(string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return false;

        if (branchName.Contains("..") ||
            branchName.Contains('~') ||
            branchName.Contains('^') ||
            branchName.Contains(':') ||
            branchName.Contains('?') ||
            branchName.Contains('*') ||
            branchName.Contains('[') ||
            branchName.Contains('\\') ||
            branchName.StartsWith('/') ||
            branchName.EndsWith('/') ||
            branchName.EndsWith(".lock") ||
            branchName.EndsWith(' '))
        {
            return false;
        }

        return true;
    }

    private bool BeValidGitUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase);
    }
}
