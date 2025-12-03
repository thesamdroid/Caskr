namespace Caskr.server.Models.Crm;

/// <summary>
/// Audit trail for all CRM sync operations.
/// Used for troubleshooting and compliance.
/// </summary>
public class CrmSyncLog
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Entity identification
    public string EntityType { get; set; } = string.Empty;  // Account, Opportunity, Contact

    public string? CaskrEntityId { get; set; }

    public string? CaskrEntityType { get; set; }  // Customer, Order, PortalUser

    public string? SalesforceId { get; set; }

    // Sync details
    public CrmSyncDirection SyncDirection { get; set; }

    public CrmSyncStatus SyncStatus { get; set; }

    public string? SyncAction { get; set; }  // Create, Update, Delete, Upsert

    // Error handling
    public string? ErrorMessage { get; set; }

    public string? ErrorCode { get; set; }

    public int RetryCount { get; set; } = 0;

    // Payload logging (for debugging)
    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    // Timing
    public DateTime SyncStartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SyncCompletedAt { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
}
