namespace Caskr.server.Models.Crm;

/// <summary>
/// OAuth tokens and connection state for CRM providers.
/// Similar pattern to AccountingIntegration.
/// </summary>
public class CrmIntegration
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Salesforce-specific fields
    public string? InstanceUrl { get; set; }

    public string? OrganizationId { get; set; }

    // OAuth tokens (encrypted)
    public string? AccessTokenEncrypted { get; set; }

    public string? RefreshTokenEncrypted { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    // Connection state
    public bool IsActive { get; set; } = true;

    public CrmConnectionStatus ConnectionStatus { get; set; } = CrmConnectionStatus.Disconnected;

    public string? LastErrorMessage { get; set; }

    public DateTime? LastErrorAt { get; set; }

    // Tracking
    public int? ConnectedByUserId { get; set; }

    public DateTime? ConnectedAt { get; set; }

    public DateTime? LastSyncAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual User? ConnectedByUser { get; set; }
}
