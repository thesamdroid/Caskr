using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Represents a feature that can be included in pricing tiers.
/// </summary>
public class PricingFeature
{
    public int Id { get; set; }

    /// <summary>
    /// Display name for the feature (e.g., "TTB Compliance Automation").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description or tooltip text for the feature.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category grouping for the feature (e.g., "Compliance", "Inventory", "Reporting", "Support").
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Display order within the category (lower numbers appear first).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this feature is currently active and visible.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<PricingTierFeature> TierFeatures { get; set; } = [];
}
