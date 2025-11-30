namespace Caskr.server.Models;

/// <summary>
/// Status of a webhook delivery attempt.
/// </summary>
public enum WebhookDeliveryStatus
{
    /// <summary>
    /// Delivery is queued and awaiting processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Delivery was successful (HTTP 2xx response).
    /// </summary>
    Success = 1,

    /// <summary>
    /// Delivery failed after exhausting all retry attempts.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Delivery failed but is scheduled for retry.
    /// </summary>
    Retrying = 3
}

/// <summary>
/// Standard webhook event types that can be subscribed to.
/// </summary>
public static class WebhookEventTypes
{
    // Barrel events
    public const string BarrelCreated = "barrel.created";
    public const string BarrelUpdated = "barrel.updated";
    public const string BarrelDeleted = "barrel.deleted";
    public const string BarrelMoved = "barrel.moved";

    // Batch events
    public const string BatchCreated = "batch.created";
    public const string BatchCompleted = "batch.completed";

    // Order events
    public const string OrderCreated = "order.created";
    public const string OrderCompleted = "order.completed";

    // Task events
    public const string TaskCreated = "task.created";
    public const string TaskCompleted = "task.completed";

    // Transfer events
    public const string TransferCreated = "transfer.created";

    // TTB events
    public const string TtbReportSubmitted = "ttb_report.submitted";

    /// <summary>
    /// Gets all valid event types for validation purposes.
    /// </summary>
    public static readonly string[] AllEventTypes =
    [
        BarrelCreated,
        BarrelUpdated,
        BarrelDeleted,
        BarrelMoved,
        BatchCreated,
        BatchCompleted,
        OrderCreated,
        OrderCompleted,
        TaskCreated,
        TaskCompleted,
        TransferCreated,
        TtbReportSubmitted
    ];

    /// <summary>
    /// Validates if the given event type is a known event type.
    /// </summary>
    public static bool IsValidEventType(string eventType)
    {
        return AllEventTypes.Contains(eventType);
    }
}
