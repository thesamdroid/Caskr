namespace Caskr.server.Models.Crm;

/// <summary>
/// Per-entity sync configuration.
/// Controls sync direction, frequency, and conflict resolution.
/// </summary>
public class CrmSyncPreference
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string Provider { get; set; } = "Salesforce";

    // Entity type this preference applies to
    public string EntityType { get; set; } = string.Empty;  // Account, Opportunity, Contact

    // Sync direction
    public CrmSyncDirection SyncDirection { get; set; } = CrmSyncDirection.Inbound;

    // Sync methods
    public bool WebhookEnabled { get; set; } = true;

    public bool PollingEnabled { get; set; } = true;

    public int PollingIntervalMinutes { get; set; } = 15;

    // Behavior options
    public bool AutoCreateEnabled { get; set; } = true;

    public bool AutoUpdateEnabled { get; set; } = true;

    public bool AutoDeleteEnabled { get; set; } = false;

    // Conflict resolution
    public string ConflictResolution { get; set; } = "LastWriteWins";

    // Tracking
    public DateTime? LastPollingAt { get; set; }

    public DateTime? LastWebhookAt { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
}
