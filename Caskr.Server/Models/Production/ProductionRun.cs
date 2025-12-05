using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a scheduled production run for distillery operations.
/// </summary>
public class ProductionRun
{
    public int Id { get; set; }

    /// <summary>
    /// Company this production run belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Name of the production run.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description of the production run.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of production activity.
    /// </summary>
    public ProductionType ProductionType { get; set; }

    /// <summary>
    /// Current status of the production run.
    /// </summary>
    public ProductionRunStatus Status { get; set; } = ProductionRunStatus.Scheduled;

    /// <summary>
    /// Scheduled start date and time.
    /// </summary>
    public DateTime ScheduledStartDate { get; set; }

    /// <summary>
    /// Scheduled end date and time.
    /// </summary>
    public DateTime ScheduledEndDate { get; set; }

    /// <summary>
    /// Actual start date and time (set when run starts).
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date and time (set when run completes).
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Optional link to an existing batch.
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Optional notes about the production run.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// User who created this production run.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual Batch? Batch { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<EquipmentBooking> EquipmentBookings { get; set; } = new List<EquipmentBooking>();

    public virtual ICollection<ProductionCalendarEvent> CalendarEvents { get; set; } = new List<ProductionCalendarEvent>();
}
