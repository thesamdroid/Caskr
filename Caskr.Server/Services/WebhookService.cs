using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
/// Service for managing webhook subscriptions and triggering webhook events.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly CaskrDbContext _dbContext;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<WebhookService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public WebhookService(
        CaskrDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        ILogger<WebhookService> logger)
    {
        _dbContext = dbContext;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task TriggerEventAsync(string eventType, int eventId, object eventData, int companyId)
    {
        if (!WebhookEventTypes.IsValidEventType(eventType))
        {
            _logger.LogWarning("Invalid webhook event type: {EventType}", eventType);
            return;
        }

        // Find all active subscriptions for this company that are subscribed to this event type
        var subscriptions = await _dbContext.WebhookSubscriptions
            .Where(s => s.CompanyId == companyId && s.IsActive)
            .ToListAsync();

        // Filter to subscriptions that include this event type in their JSONB array
        var matchingSubscriptions = subscriptions
            .Where(s => s.EventTypes.Contains(eventType))
            .ToList();

        if (matchingSubscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions for event {EventType} in company {CompanyId}",
                eventType, companyId);
            return;
        }

        // Build the webhook payload
        var payload = new WebhookPayload
        {
            EventType = eventType,
            EventId = eventId,
            Timestamp = DateTime.UtcNow,
            Data = eventData,
            CompanyId = companyId
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        // Create delivery records for each subscription
        foreach (var subscription in matchingSubscriptions)
        {
            var delivery = new WebhookDelivery
            {
                SubscriptionId = subscription.Id,
                EventType = eventType,
                EventId = eventId,
                Payload = payloadJson,
                DeliveryStatus = WebhookDeliveryStatus.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.WebhookDeliveries.Add(delivery);

            _logger.LogInformation(
                "Queued webhook delivery for subscription {SubscriptionId}, event {EventType}, entity {EventId}",
                subscription.Id, eventType, eventId);
        }

        await _dbContext.SaveChangesAsync();

        // The WebhookDeliveryWorker will pick up these pending deliveries
        _logger.LogInformation(
            "Created {Count} webhook deliveries for event {EventType} in company {CompanyId}",
            matchingSubscriptions.Count, eventType, companyId);
    }

    /// <inheritdoc />
    public async Task<WebhookSubscription> CreateSubscriptionAsync(
        int companyId,
        WebhookSubscriptionRequest request,
        int userId)
    {
        // Validate event types
        var invalidEventTypes = request.EventTypes
            .Where(et => !WebhookEventTypes.IsValidEventType(et))
            .ToList();

        if (invalidEventTypes.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid event types: {string.Join(", ", invalidEventTypes)}. " +
                $"Valid types are: {string.Join(", ", WebhookEventTypes.AllEventTypes)}");
        }

        // Validate target URL
        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var targetUri) ||
            (targetUri.Scheme != "https" && targetUri.Scheme != "http"))
        {
            throw new ArgumentException("Target URL must be a valid HTTP or HTTPS URL");
        }

        // Generate a secure secret key (32 bytes = 64 hex characters)
        var secretKey = GenerateSecretKey();

        var subscription = new WebhookSubscription
        {
            CompanyId = companyId,
            Name = request.Name,
            TargetUrl = request.TargetUrl,
            EventTypes = request.EventTypes,
            IsActive = true,
            SecretKey = secretKey,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created webhook subscription {SubscriptionId} for company {CompanyId} with events: {EventTypes}",
            subscription.Id, companyId, string.Join(", ", request.EventTypes));

        return subscription;
    }

    /// <inheritdoc />
    public async Task DeactivateSubscriptionAsync(long subscriptionId)
    {
        var subscription = await _dbContext.WebhookSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException($"Webhook subscription {subscriptionId} not found");
        }

        subscription.IsActive = false;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deactivated webhook subscription {SubscriptionId}", subscriptionId);
    }

    /// <inheritdoc />
    public async Task ReactivateSubscriptionAsync(long subscriptionId)
    {
        var subscription = await _dbContext.WebhookSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException($"Webhook subscription {subscriptionId} not found");
        }

        subscription.IsActive = true;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Reactivated webhook subscription {SubscriptionId}", subscriptionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WebhookSubscription>> GetSubscriptionsAsync(int companyId)
    {
        return await _dbContext.WebhookSubscriptions
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<WebhookSubscription?> GetSubscriptionByIdAsync(long subscriptionId)
    {
        return await _dbContext.WebhookSubscriptions
            .Include(s => s.Company)
            .Include(s => s.CreatedByUser)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WebhookDelivery>> GetRecentDeliveriesAsync(long subscriptionId, int limit = 50)
    {
        return await _dbContext.WebhookDeliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task DeleteSubscriptionAsync(long subscriptionId)
    {
        var subscription = await _dbContext.WebhookSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException($"Webhook subscription {subscriptionId} not found");
        }

        _dbContext.WebhookSubscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted webhook subscription {SubscriptionId}", subscriptionId);
    }

    /// <summary>
    /// Generates a cryptographically secure secret key for HMAC signing.
    /// </summary>
    private static string GenerateSecretKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Calculates the HMAC-SHA256 signature for a payload.
    /// </summary>
    public static string CalculateSignature(string payload, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Standard webhook payload structure sent to external endpoints.
/// </summary>
public class WebhookPayload
{
    public required string EventType { get; set; }
    public required int EventId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required object Data { get; set; }
    public required int CompanyId { get; set; }
}
