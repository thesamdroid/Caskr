using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Represents a promotional discount code that can be applied to pricing.
/// </summary>
public class PricingPromotion
{
    public int Id { get; set; }

    /// <summary>
    /// Unique promotional code that customers enter (e.g., "SUMMER20", "EARLYBIRD").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the promotion.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of discount being applied.
    /// </summary>
    [Required]
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// The discount value. Interpretation depends on DiscountType:
    /// - Percentage: 20 = 20% off
    /// - FixedAmount: 5000 = $50 off (in cents)
    /// - FreeMonths: 2 = 2 free months
    /// </summary>
    public int DiscountValue { get; set; }

    /// <summary>
    /// JSON array of tier IDs this promotion applies to.
    /// Null means the promotion applies to all tiers.
    /// Example: [1, 2, 3] to apply to specific tiers only.
    /// </summary>
    public string? AppliesToTiersJson { get; set; }

    /// <summary>
    /// When the promotion becomes valid (null = immediately valid).
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// When the promotion expires (null = never expires).
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Maximum number of times this promo code can be redeemed (null = unlimited).
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Current number of times this promo code has been redeemed.
    /// </summary>
    public int CurrentRedemptions { get; set; }

    /// <summary>
    /// Whether this promotion is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
