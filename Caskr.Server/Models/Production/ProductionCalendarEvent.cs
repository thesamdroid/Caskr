using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents an event on the production calendar.
/// </summary>
public class ProductionCalendarEvent
{
    public int Id { get; set; }

    /// <summary>
    /// Company this calendar event belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Title of the calendar event.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// Type of calendar event.
    /// </summary>
    public CalendarEventType EventType { get; set; }

    /// <summary>
    /// Start date and time of the event.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date and time of the event.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether this is an all-day event.
    /// </summary>
    public bool AllDay { get; set; }

    /// <summary>
    /// Optional hex color code for display (e.g., "#FF6B6B").
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Optional link to a production run if this event represents one.
    /// </summary>
    public int? ProductionRunId { get; set; }

    /// <summary>
    /// User who created this calendar event.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual ProductionRun? ProductionRun { get; set; }

    public virtual User? CreatedByUser { get; set; }
}
