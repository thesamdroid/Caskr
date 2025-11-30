using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

/// <summary>
/// Represents a user's saved report configuration with pre-selected filters.
/// Allows users to quickly re-run reports with their preferred filter settings.
/// </summary>
public class SavedReport
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ReportTemplateId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON object containing the saved filter values.
    /// E.g., {"startDate": "2024-01-01", "endDate": "2024-12-31", "status": ["Active", "Aging"]}
    /// </summary>
    public string? FilterValues { get; set; }

    /// <summary>
    /// Whether this saved report is marked as a favorite for quick access.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Last time this saved report was executed.
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Number of times this saved report has been executed.
    /// </summary>
    public int RunCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ReportTemplate ReportTemplate { get; set; } = null!;
}
