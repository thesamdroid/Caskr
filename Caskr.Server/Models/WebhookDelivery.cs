namespace Caskr.server.Models;

/// <summary>
/// Records a webhook delivery attempt including status and retry information.
/// </summary>
public class WebhookDelivery
{
    public long Id { get; set; }

    public long SubscriptionId { get; set; }

    /// <summary>
    /// The type of event being delivered (e.g., "barrel.created").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity that triggered this event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Full webhook payload as JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public WebhookDeliveryStatus DeliveryStatus { get; set; } = WebhookDeliveryStatus.Pending;

    /// <summary>
    /// HTTP status code from the last delivery attempt.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Response body from the last delivery attempt (truncated if large).
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Scheduled time for the next retry attempt.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Timestamp when the webhook was successfully delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public virtual WebhookSubscription Subscription { get; set; } = null!;
}
