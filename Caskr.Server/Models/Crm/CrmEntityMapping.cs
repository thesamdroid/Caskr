namespace Caskr.server.Models.Crm;

/// <summary>
/// Links Caskr entities to their Salesforce counterparts.
/// Used for update detection and conflict resolution.
/// </summary>
public class CrmEntityMapping
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Salesforce entity info
    public string SalesforceEntityType { get; set; } = string.Empty;  // Account, Opportunity, Contact

    public string SalesforceId { get; set; } = string.Empty;

    // Caskr entity info
    public string CaskrEntityType { get; set; } = string.Empty;  // Customer, Order, PortalUser

    public string CaskrEntityId { get; set; } = string.Empty;

    // Sync tracking for conflict detection
    public DateTime? LastSyncAt { get; set; }

    public DateTime? CaskrLastModified { get; set; }

    public DateTime? SalesforceLastModified { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
}
