namespace Caskr.server.Models.Portal;

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

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<CaskOwnership> CaskOwnerships { get; set; } = new List<CaskOwnership>();

    public virtual ICollection<PortalAccessLog> AccessLogs { get; set; } = new List<PortalAccessLog>();

    public virtual ICollection<PortalNotification> Notifications { get; set; } = new List<PortalNotification>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}".Trim();
}
