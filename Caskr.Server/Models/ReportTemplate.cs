using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

/// <summary>
/// Represents a reusable report template that defines how to query and display data.
/// Templates are used by the ReportingService to dynamically generate SQL queries.
/// </summary>
public class ReportTemplate
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON array of table names to query. E.g., ["barrels", "batches", "orders"]
    /// </summary>
    [Required]
    public string DataSources { get; set; } = "[]";

    /// <summary>
    /// JSON array of column specifications. E.g., ["barrels.sku", "batches.name", "barrels.created_at"]
    /// Supports aliases: "barrels.sku as BarrelSku"
    /// </summary>
    [Required]
    public string Columns { get; set; } = "[]";

    /// <summary>
    /// JSON object defining filter conditions. E.g.:
    /// {"filter": "barrels.status = @status AND barrels.created_at >= @startDate", "defaultParameters": {"status": "Aging"}}
    /// </summary>
    public string? Filters { get; set; }

    /// <summary>
    /// JSON array of columns to group by. E.g., ["batches.id", "batches.name"]
    /// </summary>
    public string? Groupings { get; set; }

    /// <summary>
    /// JSON array of sort specifications. E.g., [{"column": "barrels.created_at", "direction": "desc"}]
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// Default page size for paginated results.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Whether this template is active and can be executed.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this template is a system template (cannot be deleted by users).
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// User who created this template.
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
}

/// <summary>
/// Status enum for report templates.
/// </summary>
public enum ReportTemplateStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}
