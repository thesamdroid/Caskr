using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models.Production;

/// <summary>
/// Represents production equipment owned by a distillery.
/// </summary>
public class Equipment
{
    public int Id { get; set; }

    /// <summary>
    /// Company this equipment belongs to.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Name of the equipment.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Type of equipment.
    /// </summary>
    public EquipmentType EquipmentType { get; set; }

    /// <summary>
    /// Capacity of the equipment (e.g., 500 gallons).
    /// </summary>
    public decimal? Capacity { get; set; }

    /// <summary>
    /// Unit of capacity measurement (e.g., "gallons", "liters").
    /// </summary>
    [MaxLength(20)]
    public string? CapacityUnit { get; set; }

    /// <summary>
    /// Physical location of the equipment.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Whether the equipment is currently active and available for booking.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes about maintenance requirements or history.
    /// </summary>
    [MaxLength(1000)]
    public string? MaintenanceNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<EquipmentBooking> Bookings { get; set; } = new List<EquipmentBooking>();
}
