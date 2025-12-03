namespace Caskr.server.Models.Portal;

/// <summary>
/// Audit trail for all portal user actions - security and compliance logging
/// </summary>
public class PortalAccessLog
{
    public long Id { get; set; }

    public long PortalUserId { get; set; }

    public PortalAction Action { get; set; }

    public string? ResourceType { get; set; }

    public long? ResourceId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual PortalUser PortalUser { get; set; } = null!;
}

public enum PortalAction
{
    Login,
    Login_Failed,
    Logout,
    View_Barrel,
    View_Dashboard,
    Download_Certificate,
    Download_Document,
    View_Document,
    Password_Reset_Request,
    Password_Reset_Complete,
    Profile_Update
}
