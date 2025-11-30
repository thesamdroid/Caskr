namespace Caskr.server.Models;

/// <summary>
/// Represents a webhook subscription for receiving event notifications.
/// </summary>
public class WebhookSubscription
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    /// <summary>
    /// Human-readable name for the subscription.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// HTTPS endpoint URL to receive webhook POST requests.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// List of event types this subscription listens to.
    /// </summary>
    public List<string> EventTypes { get; set; } = [];

    /// <summary>
    /// Whether this subscription is active and should receive events.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Secret key used to sign webhook payloads with HMAC-SHA256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}
