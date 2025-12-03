namespace Caskr.server.Models.Crm;

/// <summary>
/// Customizable field mappings between Salesforce and Caskr entities.
/// Allows per-company configuration.
/// </summary>
public class CrmFieldMapping
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Entity type this mapping applies to
    public string SalesforceEntityType { get; set; } = string.Empty;  // Account, Opportunity, Contact

    public string CaskrEntityType { get; set; } = string.Empty;  // Customer, Order, PortalUser

    // Field mapping
    public string SalesforceField { get; set; } = string.Empty;  // Salesforce API field name

    public string CaskrField { get; set; } = string.Empty;  // Caskr entity field name

    // Transformation options
    public string? TransformationRule { get; set; }  // UPPERCASE, LOWERCASE, DATE_FORMAT, etc.

    public string? DefaultValue { get; set; }

    // Configuration
    public CrmSyncDirection SyncDirection { get; set; } = CrmSyncDirection.Inbound;

    public bool IsRequired { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
}
