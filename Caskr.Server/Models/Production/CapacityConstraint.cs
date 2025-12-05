using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a capacity constraint for limiting production capacity.
/// </summary>
public class CapacityConstraint
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The company this constraint belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// The equipment this constraint applies to. Null means company-wide constraint.
    /// </summary>
    public int? EquipmentId { get; set; }

    /// <summary>
    /// Type of constraint.
    /// </summary>
    public CapacityConstraintType ConstraintType { get; set; }

    /// <summary>
    /// The constraint value (e.g., max hours, max runs).
    /// </summary>
    public decimal ConstraintValue { get; set; }

    /// <summary>
    /// When the constraint becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// When the constraint expires. Null means indefinitely.
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Reason for the constraint.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether the constraint is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The user who created this constraint.
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
    /// The company this constraint belongs to.
    /// </summary>
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// The equipment this constraint applies to (null for company-wide).
    /// </summary>
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// The user who created this constraint.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }
}
