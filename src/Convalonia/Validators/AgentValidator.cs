using System;
using System.Linq;
using FluentValidation;
using Convalonia.Models;

namespace Convalonia.Validators;

/// <summary>
/// Validates Agent model properties
/// </summary>
public class AgentValidator : AbstractValidator<Agent>
{
    private static readonly string[] ValidModels =
    {
        "claude-sonnet-4-5-20250929",
        "claude-opus-4-20250514",
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022",
        "claude-3-opus-20240229",
        "claude-3-sonnet-20240229",
        "claude-3-haiku-20240307"
    };

    public AgentValidator()
    {
        RuleFor(a => a.Id)
            .NotEmpty()
            .WithMessage("Agent ID cannot be empty");

        RuleFor(a => a.Name)
            .NotEmpty()
            .WithMessage("Agent name is required")
            .MaximumLength(255)
            .WithMessage("Agent name must not exceed 255 characters");

        RuleFor(a => a.WorkspaceId)
            .NotEmpty()
            .WithMessage("Agent must be associated with a workspace");

        RuleFor(a => a.Model)
            .NotEmpty()
            .WithMessage("Model name is required")
            .Must(BeValidModel)
            .WithMessage($"Model must be one of: {string.Join(", ", ValidModels)}");

        RuleFor(a => a.CreatedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Created date cannot be in the future");

        RuleFor(a => a.CompletedAt)
            .GreaterThan(a => a.CreatedAt)
            .When(a => a.CompletedAt.HasValue)
            .WithMessage("Completed date must be after created date");

        RuleFor(a => a.Status)
            .IsInEnum()
            .WithMessage("Invalid agent status");
    }

    private bool BeValidModel(string model)
    {
        return ValidModels.Contains(model, StringComparer.OrdinalIgnoreCase);
    }
}
