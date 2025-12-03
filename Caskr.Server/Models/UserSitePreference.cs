using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

/// <summary>
/// User preference for site version (desktop vs mobile)
/// </summary>
public enum SitePreference
{
    Auto = 0,
    Desktop = 1,
    Mobile = 2
}

/// <summary>
/// Tracks user preferences for site version (desktop/mobile) routing
/// </summary>
[Table("user_site_preferences")]
public class UserSitePreference
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// User ID for authenticated users (nullable for anonymous)
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// Session ID for anonymous users
    /// </summary>
    [Column("session_id")]
    [MaxLength(255)]
    public string? SessionId { get; set; }

    /// <summary>
    /// User's preferred site version
    /// </summary>
    [Column("preferred_site")]
    public SitePreference PreferredSite { get; set; } = SitePreference.Auto;

    /// <summary>
    /// Last detected device type from User-Agent
    /// </summary>
    [Column("last_detected_device")]
    [MaxLength(500)]
    public string? LastDetectedDevice { get; set; }

    /// <summary>
    /// Timestamp when preference was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when preference was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for User
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
