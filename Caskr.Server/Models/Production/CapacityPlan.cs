using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a capacity planning period for production scheduling.
/// </summary>
public class CapacityPlan
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The company this capacity plan belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Name of the capacity plan.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Description of the capacity plan.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Start of the planning period.
    /// </summary>
    public DateTime PlanPeriodStart { get; set; }

    /// <summary>
    /// End of the planning period.
    /// </summary>
    public DateTime PlanPeriodEnd { get; set; }

    /// <summary>
    /// Type of the capacity plan (Weekly, Monthly, Quarterly, Annual).
    /// </summary>
    public CapacityPlanType PlanType { get; set; }

    /// <summary>
    /// Status of the capacity plan.
    /// </summary>
    public CapacityPlanStatus Status { get; set; } = CapacityPlanStatus.Draft;

    /// <summary>
    /// Target production in proof gallons.
    /// </summary>
    public decimal? TargetProofGallons { get; set; }

    /// <summary>
    /// Target number of bottles to produce.
    /// </summary>
    public int? TargetBottles { get; set; }

    /// <summary>
    /// Target number of batches to complete.
    /// </summary>
    public int? TargetBatches { get; set; }

    /// <summary>
    /// Additional notes about the capacity plan.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// The user who created this plan.
    /// </summary>
    public int? CreatedByUserId { get; set; }

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
    /// The company this plan belongs to.
    /// </summary>
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// The user who created this plan.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// The capacity allocations associated with this plan.
    /// </summary>
    public virtual ICollection<CapacityAllocation> Allocations { get; set; } = new List<CapacityAllocation>();
}
