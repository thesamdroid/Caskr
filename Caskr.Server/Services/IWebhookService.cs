using Caskr.server.Models;

namespace Caskr.Server.Services;

/// <summary>
/// Defines operations for managing webhook subscriptions and triggering webhook events.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Triggers a webhook event for all active subscriptions that are subscribed to the given event type.
    /// This method queues the delivery for async processing and returns immediately.
    /// </summary>
    /// <param name="eventType">The type of event (e.g., "barrel.created").</param>
    /// <param name="eventId">The ID of the entity that triggered the event.</param>
    /// <param name="eventData">The entity data to include in the webhook payload.</param>
    /// <param name="companyId">The company ID for which to trigger webhooks.</param>
    Task TriggerEventAsync(string eventType, int eventId, object eventData, int companyId);

    /// <summary>
    /// Creates a new webhook subscription for the specified company.
    /// </summary>
    /// <param name="companyId">The company ID to create the subscription for.</param>
    /// <param name="request">The subscription configuration details.</param>
    /// <param name="userId">The ID of the user creating the subscription.</param>
    /// <returns>The created webhook subscription.</returns>
    Task<WebhookSubscription> CreateSubscriptionAsync(int companyId, WebhookSubscriptionRequest request, int userId);

    /// <summary>
    /// Deactivates a webhook subscription, stopping future event deliveries.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to deactivate.</param>
    Task DeactivateSubscriptionAsync(long subscriptionId);

    /// <summary>
    /// Reactivates a previously deactivated webhook subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to reactivate.</param>
    Task ReactivateSubscriptionAsync(long subscriptionId);

    /// <summary>
    /// Gets all webhook subscriptions for the specified company.
    /// </summary>
    /// <param name="companyId">The company ID to get subscriptions for.</param>
    /// <returns>Collection of webhook subscriptions.</returns>
    Task<IEnumerable<WebhookSubscription>> GetSubscriptionsAsync(int companyId);

    /// <summary>
    /// Gets a specific webhook subscription by ID.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>The webhook subscription, or null if not found.</returns>
    Task<WebhookSubscription?> GetSubscriptionByIdAsync(long subscriptionId);

    /// <summary>
    /// Gets recent deliveries for a subscription for monitoring purposes.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="limit">Maximum number of deliveries to return.</param>
    /// <returns>Collection of recent webhook deliveries.</returns>
    Task<IEnumerable<WebhookDelivery>> GetRecentDeliveriesAsync(long subscriptionId, int limit = 50);

    /// <summary>
    /// Deletes a webhook subscription and all associated delivery records.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    Task DeleteSubscriptionAsync(long subscriptionId);
}

/// <summary>
/// Request model for creating a webhook subscription.
/// </summary>
public class WebhookSubscriptionRequest
{
    /// <summary>
    /// Human-readable name for the subscription.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// HTTPS endpoint URL to receive webhook POST requests.
    /// </summary>
    public required string TargetUrl { get; set; }

    /// <summary>
    /// List of event types to subscribe to.
    /// </summary>
    public required List<string> EventTypes { get; set; }
}
