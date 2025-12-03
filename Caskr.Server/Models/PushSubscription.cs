using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

/// <summary>
/// Represents a Web Push notification subscription for a user's device
/// </summary>
[Table("push_subscriptions")]
public class PushSubscription
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// User ID this subscription belongs to
    /// </summary>
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Push service endpoint URL
    /// </summary>
    [Column("endpoint")]
    [Required]
    [MaxLength(2000)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// P256DH key for encryption (Base64 encoded)
    /// </summary>
    [Column("p256dh_key")]
    [Required]
    [MaxLength(500)]
    public string P256dhKey { get; set; } = string.Empty;

    /// <summary>
    /// Auth key for encryption (Base64 encoded)
    /// </summary>
    [Column("auth_key")]
    [Required]
    [MaxLength(500)]
    public string AuthKey { get; set; } = string.Empty;

    /// <summary>
    /// User agent string of the subscribed device
    /// </summary>
    [Column("user_agent")]
    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device name/description for user reference
    /// </summary>
    [Column("device_name")]
    [MaxLength(255)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Whether this subscription is currently active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when subscription was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when subscription was last used to send a notification
    /// </summary>
    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Timestamp when subscription expires (if known from push service)
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Number of consecutive delivery failures
    /// </summary>
    [Column("failure_count")]
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Timestamp of last delivery failure
    /// </summary>
    [Column("last_failure_at")]
    public DateTime? LastFailureAt { get; set; }

    /// <summary>
    /// Navigation property for User
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// User notification preferences
/// </summary>
[Table("notification_preferences")]
public class NotificationPreference
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// User ID this preference belongs to
    /// </summary>
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Master toggle for all notifications
    /// </summary>
    [Column("notifications_enabled")]
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Notify when assigned to a task
    /// </summary>
    [Column("task_assignments")]
    public bool TaskAssignments { get; set; } = true;

    /// <summary>
    /// Notify when task due date is approaching
    /// </summary>
    [Column("task_reminders")]
    public bool TaskReminders { get; set; } = true;

    /// <summary>
    /// Notify on compliance-related alerts
    /// </summary>
    [Column("compliance_alerts")]
    public bool ComplianceAlerts { get; set; } = true;

    /// <summary>
    /// Notify on sync status changes
    /// </summary>
    [Column("sync_status")]
    public bool SyncStatus { get; set; } = true;

    /// <summary>
    /// Start of quiet hours (local time, null = no quiet hours)
    /// </summary>
    [Column("quiet_hours_start")]
    public TimeOnly? QuietHoursStart { get; set; }

    /// <summary>
    /// End of quiet hours (local time)
    /// </summary>
    [Column("quiet_hours_end")]
    public TimeOnly? QuietHoursEnd { get; set; }

    /// <summary>
    /// User's timezone for quiet hours calculation
    /// </summary>
    [Column("timezone")]
    [MaxLength(100)]
    public string? Timezone { get; set; }

    /// <summary>
    /// Timestamp when preferences were last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for User
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// Types of notifications that can be sent
/// </summary>
public enum NotificationType
{
    TaskAssigned = 1,
    TaskDueSoon = 2,
    TaskUrgent = 3,
    ComplianceReportDue = 4,
    ComplianceRequiresApproval = 5,
    ComplianceApproved = 6,
    ComplianceRejected = 7,
    SyncCompleted = 8,
    SyncFailed = 9,
    General = 10
}
