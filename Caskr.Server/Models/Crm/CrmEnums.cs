namespace Caskr.server.Models.Crm;

/// <summary>
/// Sync direction for CRM operations.
/// </summary>
public enum CrmSyncDirection
{
    Inbound,       // Salesforce → Caskr
    Outbound,      // Caskr → Salesforce
    Bidirectional  // Both directions
}

/// <summary>
/// Status of a CRM sync operation.
/// </summary>
public enum CrmSyncStatus
{
    Pending,
    InProgress,
    Success,
    Failed,
    Conflict
}

/// <summary>
/// Status of conflict resolution.
/// </summary>
public enum CrmConflictStatus
{
    Pending,
    Resolved_Caskr,
    Resolved_Salesforce,
    Merged
}

/// <summary>
/// Customer type classification for CRM.
/// </summary>
public enum CustomerType
{
    OnPremise,   // Bars, restaurants
    OffPremise,  // Retail stores
    Distributor, // Wholesale distributors
    Direct,      // Direct to consumer
    Investor     // Cask investors
}

/// <summary>
/// Connection status for CRM integrations.
/// </summary>
public enum CrmConnectionStatus
{
    Connected,
    Disconnected,
    Error,
    TokenExpired
}
