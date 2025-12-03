using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Represents a frequently asked question displayed on the pricing page.
/// </summary>
public class PricingFaq
{
    public int Id { get; set; }

    /// <summary>
    /// The FAQ question text.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The FAQ answer text (supports markdown formatting).
    /// </summary>
    [Required]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Display order (lower numbers appear first).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this FAQ is currently active and visible.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
