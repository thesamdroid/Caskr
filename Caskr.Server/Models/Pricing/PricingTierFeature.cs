using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Junction table linking pricing tiers to features with additional metadata.
/// </summary>
public class PricingTierFeature
{
    public int Id { get; set; }

    /// <summary>
    /// The pricing tier this feature belongs to.
    /// </summary>
    [Required]
    public int TierId { get; set; }

    /// <summary>
    /// The feature being linked.
    /// </summary>
    [Required]
    public int FeatureId { get; set; }

    /// <summary>
    /// Whether this feature is included in the tier (true = checkmark, false = X).
    /// </summary>
    public bool IsIncluded { get; set; } = true;

    /// <summary>
    /// Limit value for tiered features (e.g., "5 users", "Unlimited", "10K cases/year").
    /// </summary>
    [MaxLength(50)]
    public string? LimitValue { get; set; }

    /// <summary>
    /// Additional context about the limit (e.g., "per month", "included with add-on").
    /// </summary>
    [MaxLength(100)]
    public string? LimitDescription { get; set; }

    // Navigation properties
    public virtual PricingTier Tier { get; set; } = null!;

    public virtual PricingFeature Feature { get; set; } = null!;
}
