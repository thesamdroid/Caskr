using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

/// <summary>
/// Audit log entity for tracking all changes to TTB compliance data.
/// This log is immutable and provides a complete audit trail for TTB inspections.
/// </summary>
public class TtbAuditLog
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// The type of entity that was changed (TtbTransaction, TtbMonthlyReport, TtbGaugeRecord, TtbTaxDetermination).
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
    /// The action that was performed (Create, Update, Delete).
    /// </summary>
    [Required]
    public TtbAuditAction Action { get; set; }

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
    public virtual Company Company { get; set; } = null!;
    public virtual User ChangedByUser { get; set; } = null!;
}

/// <summary>
/// The type of audit action performed.
/// </summary>
public enum TtbAuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2
}
