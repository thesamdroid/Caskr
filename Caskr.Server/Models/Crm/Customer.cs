namespace Caskr.server.Models.Crm;

/// <summary>
/// Master customer record for CRM integration.
/// Stores customer/account data synced from Salesforce.
/// </summary>
public class Customer
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public CustomerType CustomerType { get; set; } = CustomerType.Direct;

    // Contact information
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    // Address information
    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; } = "USA";

    // Salesforce integration fields
    public string? SalesforceAccountId { get; set; }

    public DateTime? SalesforceLastSyncAt { get; set; }

    // Ownership and tracking
    public int? AssignedUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual User? AssignedUser { get; set; }
}
