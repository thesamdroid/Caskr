using System.Collections.Generic;
using System.Linq;

namespace Caskr.server.Models;

/// <summary>
/// Represents the result of TTB report data validation.
/// Contains errors that must be fixed before submission and warnings that should be reviewed.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed without errors.
    /// Warnings do not affect this flag.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors that must be fixed before submission.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Gets the list of validation warnings that should be reviewed but do not block submission.
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Creates a new validation result with no errors or warnings (valid).
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a new validation result with the specified error.
    /// </summary>
    public static ValidationResult WithError(string error) => new() { Errors = new List<string> { error } };

    /// <summary>
    /// Creates a new validation result with the specified warning.
    /// </summary>
    public static ValidationResult WithWarning(string warning) => new() { Warnings = new List<string> { warning } };

    /// <summary>
    /// Adds an error to this validation result.
    /// </summary>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// Adds a warning to this validation result.
    /// </summary>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Returns true if there are any errors or warnings.
    /// </summary>
    public bool HasIssues => Errors.Any() || Warnings.Any();
}
