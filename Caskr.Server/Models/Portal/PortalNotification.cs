namespace Caskr.server.Models.Portal;

/// <summary>
/// In-app notifications for portal users about their barrel investments
/// </summary>
public class PortalNotification
{
    public long Id { get; set; }

    public long PortalUserId { get; set; }

    public PortalNotificationType NotificationType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int? RelatedBarrelId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual PortalUser PortalUser { get; set; } = null!;

    public virtual Barrel? RelatedBarrel { get; set; }
}

public enum PortalNotificationType
{
    Barrel_Milestone,
    Maturation_Update,
    Ready_For_Bottling,
    Document_Available,
    New_Photo,
    Account_Update,
    System_Message
}
