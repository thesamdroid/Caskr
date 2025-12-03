namespace Caskr.server.Models.Portal;

/// <summary>
/// Links portal customers to their barrel investments.
/// Supports fractional ownership via ownership_percentage.
/// </summary>
public class CaskOwnership
{
    public long Id { get; set; }

    public long PortalUserId { get; set; }

    public int BarrelId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// Percentage of barrel owned (allows fractional ownership, e.g., 25.00 = 25%)
    /// </summary>
    public decimal OwnershipPercentage { get; set; } = 100.00m;

    public string? CertificateNumber { get; set; }

    public CaskOwnershipStatus Status { get; set; } = CaskOwnershipStatus.Active;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual PortalUser PortalUser { get; set; } = null!;

    public virtual Barrel Barrel { get; set; } = null!;

    public virtual ICollection<PortalDocument> Documents { get; set; } = new List<PortalDocument>();
}

public enum CaskOwnershipStatus
{
    Active,
    Matured,
    Bottled,
    Sold
}
