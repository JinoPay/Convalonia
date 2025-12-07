using System;
using System.IO;
using FluentValidation;
using Convalonia.Models;

namespace Convalonia.Validators;

/// <summary>
/// Validates Repository model properties
/// </summary>
public class RepositoryValidator : AbstractValidator<Repository>
{
    public RepositoryValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithMessage("Repository ID cannot be empty");

        RuleFor(r => r.Name)
            .NotEmpty()
            .WithMessage("Repository name is required")
            .MaximumLength(255)
            .WithMessage("Repository name must not exceed 255 characters")
            .Matches(@"^[a-zA-Z0-9_\-\. ]+$")
            .WithMessage("Repository name can only contain alphanumeric characters, spaces, hyphens, underscores, and dots");

        RuleFor(r => r.WorkspacePath)
            .NotEmpty()
            .WithMessage("Workspace path is required")
            .Must(BeValidPath)
            .WithMessage("Workspace path must be a valid absolute path");

        RuleFor(r => r.RootPath)
            .Must(BeValidPath!)
            .When(r => !string.IsNullOrEmpty(r.RootPath))
            .WithMessage("Root path must be a valid absolute path");

        RuleFor(r => r.CurrentBranch)
            .Must(BeValidBranchName!)
            .When(r => !string.IsNullOrEmpty(r.CurrentBranch))
            .WithMessage("Invalid branch name");

        RuleFor(r => r.BaseBranch)
            .Must(BeValidBranchName!)
            .When(r => !string.IsNullOrEmpty(r.BaseBranch))
            .WithMessage("Invalid base branch name");

        RuleFor(r => r.RemoteOrigin)
            .Must(BeValidGitUrl!)
            .When(r => !string.IsNullOrEmpty(r.RemoteOrigin))
            .WithMessage("Invalid Git URL format");
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

        // Git branch naming rules:
        // - Cannot contain: .., ~, ^, :, ?, *, [, \, space at end, consecutive dots
        // - Cannot start or end with /
        // - Cannot end with .lock
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

        // Support HTTPS and SSH URLs
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase);
    }
}
