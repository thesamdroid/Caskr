using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Represents a pricing tier (e.g., Craft, Growth, Professional, Enterprise).
/// </summary>
public class PricingTier
{
    public int Id { get; set; }

    /// <summary>
    /// Display name for the tier (e.g., "Craft", "Growth", "Professional", "Enterprise").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (e.g., "craft", "growth", "professional", "enterprise").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Short description or tagline (e.g., "Perfect for craft distilleries").
    /// </summary>
    [MaxLength(200)]
    public string? Tagline { get; set; }

    /// <summary>
    /// Monthly price in cents (nullable for custom pricing tiers like Enterprise).
    /// </summary>
    public int? MonthlyPriceCents { get; set; }

    /// <summary>
    /// Annual price in cents (nullable for custom pricing).
    /// </summary>
    public int? AnnualPriceCents { get; set; }

    /// <summary>
    /// Percentage discount for annual billing (e.g., 20 for "Save 20%").
    /// </summary>
    public int AnnualDiscountPercent { get; set; }

    /// <summary>
    /// Whether this tier should be highlighted as "Most Popular".
    /// </summary>
    public bool IsPopular { get; set; }

    /// <summary>
    /// Whether this tier uses custom/contact-based pricing (true for Enterprise tier).
    /// </summary>
    public bool IsCustomPricing { get; set; }

    /// <summary>
    /// Call-to-action button text (e.g., "Start Free Trial", "Contact Sales").
    /// </summary>
    [MaxLength(50)]
    public string? CtaText { get; set; }

    /// <summary>
    /// Call-to-action button URL (e.g., "/signup?plan=craft", "/contact").
    /// </summary>
    [MaxLength(200)]
    public string? CtaUrl { get; set; }

    /// <summary>
    /// Display order (lower numbers appear first).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this tier is currently active and visible on the pricing page.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<PricingTierFeature> TierFeatures { get; set; } = [];
}
