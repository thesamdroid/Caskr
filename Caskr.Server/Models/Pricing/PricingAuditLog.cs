using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Pricing;

/// <summary>
/// Audit log for tracking all changes to pricing data by administrators.
/// </summary>
public class PricingAuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// The type of entity that was changed (PricingTier, PricingFeature, PricingFaq, PricingPromotion).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity that was changed.
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    [Required]
    public PricingAuditAction Action { get; set; }

    /// <summary>
    /// The user who made the change.
    /// </summary>
    [Required]
    public int ChangedByUserId { get; set; }

    /// <summary>
    /// UTC timestamp when the change was made.
    /// </summary>
    [Required]
    public DateTime ChangeTimestamp { get; set; }

    /// <summary>
    /// JSON representation of the old values before the change (null for Create actions).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of the new values after the change (null for Delete actions).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// The IP address from which the change was made.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string of the client that made the change.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Human-readable description of the change for display purposes.
    /// </summary>
    public string? ChangeDescription { get; set; }

    // Navigation properties
    public virtual User ChangedByUser { get; set; } = null!;
}
