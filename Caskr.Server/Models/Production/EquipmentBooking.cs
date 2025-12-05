using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents a booking of equipment for a production run.
/// </summary>
public class EquipmentBooking
{
    public int Id { get; set; }

    /// <summary>
    /// The production run this booking is for.
    /// </summary>
    public int ProductionRunId { get; set; }

    /// <summary>
    /// The equipment being booked.
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// Start time of the booking.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the booking.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Status of the booking.
    /// </summary>
    public EquipmentBookingStatus Status { get; set; } = EquipmentBookingStatus.Tentative;

    /// <summary>
    /// Optional notes about the booking.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ProductionRun ProductionRun { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;
}
