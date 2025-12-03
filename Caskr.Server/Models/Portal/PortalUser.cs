namespace Caskr.server.Models.Portal;

using Caskr.server.Models.Crm;

/// <summary>
/// Customer portal user - separate from main app users for security isolation.
/// Allows external customers to view their cask investments.
/// </summary>
public class PortalUser
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int CompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailVerified { get; set; } = false;

    public string? VerificationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockoutUntil { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Salesforce CRM integration fields (CRM-001)
    /// <summary>
    /// Salesforce Contact ID (18 character format)
    /// </summary>
    public string? SalesforceContactId { get; set; }

    /// <summary>
    /// Timestamp of last Salesforce sync
    /// </summary>
    public DateTime? SalesforceLastSyncAt { get; set; }

    /// <summary>
    /// Link to the customer record for this portal user
    /// </summary>
    public long? LinkedCustomerId { get; set; }

    /// <summary>
    /// Flag indicating if this user is a cask investor (from Salesforce custom field)
    /// </summary>
    public bool IsCaskInvestor { get; set; } = false;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Linked customer record (for CRM integration)
    /// </summary>
    public virtual Customer? LinkedCustomer { get; set; }

    public virtual ICollection<CaskOwnership> CaskOwnerships { get; set; } = new List<CaskOwnership>();

    public virtual ICollection<PortalAccessLog> AccessLogs { get; set; } = new List<PortalAccessLog>();

    public virtual ICollection<PortalNotification> Notifications { get; set; } = new List<PortalNotification>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}".Trim();
}
