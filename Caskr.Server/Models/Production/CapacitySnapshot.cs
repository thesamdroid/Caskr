namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a historical snapshot of capacity metrics for tracking trends.
/// </summary>
public class CapacitySnapshot
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The company this snapshot belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// The date of the snapshot.
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// The equipment this snapshot is for.
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// Total available capacity hours.
    /// </summary>
    public decimal TotalCapacityHours { get; set; }

    /// <summary>
    /// Hours allocated to production.
    /// </summary>
    public decimal AllocatedHours { get; set; }

    /// <summary>
    /// Hours allocated to maintenance.
    /// </summary>
    public decimal MaintenanceHours { get; set; }

    /// <summary>
    /// Calculated utilization percentage.
    /// </summary>
    public decimal UtilizationPercent { get; set; }

    /// <summary>
    /// Planned proof gallons for the period.
    /// </summary>
    public decimal? PlannedProofGallons { get; set; }

    /// <summary>
    /// Actual proof gallons produced.
    /// </summary>
    public decimal? ActualProofGallons { get; set; }

    /// <summary>
    /// When this snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The company this snapshot belongs to.
    /// </summary>
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// The equipment this snapshot is for.
    /// </summary>
    public virtual Equipment Equipment { get; set; } = null!;
}
