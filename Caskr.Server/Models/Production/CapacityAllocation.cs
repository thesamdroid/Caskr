using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a capacity allocation for equipment within a capacity plan.
/// </summary>
public class CapacityAllocation
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The capacity plan this allocation belongs to.
    /// </summary>
    public int CapacityPlanId { get; set; }

    /// <summary>
    /// The equipment being allocated.
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// Type of allocation (Production, Maintenance, Buffer, Reserved).
    /// </summary>
    public CapacityAllocationType AllocationType { get; set; }

    /// <summary>
    /// Start date of the allocation.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the allocation.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Number of hours allocated.
    /// </summary>
    public decimal HoursAllocated { get; set; }

    /// <summary>
    /// Type of production activity (if allocation is for production).
    /// </summary>
    public ProductionType? ProductionType { get; set; }

    /// <summary>
    /// Additional notes about the allocation.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The capacity plan this allocation belongs to.
    /// </summary>
    public virtual CapacityPlan CapacityPlan { get; set; } = null!;

    /// <summary>
    /// The equipment being allocated.
    /// </summary>
    public virtual Equipment Equipment { get; set; } = null!;
}
