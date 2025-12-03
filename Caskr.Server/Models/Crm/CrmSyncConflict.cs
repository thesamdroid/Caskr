namespace Caskr.server.Models.Crm;

/// <summary>
/// Queue for conflicts requiring manual resolution.
/// Shows side-by-side values for user decision.
/// </summary>
public class CrmSyncConflict
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Entity identification
    public string SalesforceEntityType { get; set; } = string.Empty;

    public string SalesforceId { get; set; } = string.Empty;

    public string CaskrEntityType { get; set; } = string.Empty;

    public string CaskrEntityId { get; set; } = string.Empty;

    // Conflict details
    public string FieldName { get; set; } = string.Empty;

    public string? CaskrValue { get; set; }

    public string? SalesforceValue { get; set; }

    public DateTime? CaskrModifiedAt { get; set; }

    public DateTime? SalesforceModifiedAt { get; set; }

    // Resolution
    public CrmConflictStatus ResolutionStatus { get; set; } = CrmConflictStatus.Pending;

    public string? ResolvedValue { get; set; }

    public int? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual User? ResolvedByUser { get; set; }
}
