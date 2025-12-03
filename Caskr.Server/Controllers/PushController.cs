using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

#region Request/Response DTOs

/// <summary>
/// Request to subscribe to push notifications
/// </summary>
public class PushSubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}

/// <summary>
/// Request to unsubscribe from push notifications
/// </summary>
public class PushUnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>
/// Response for a push subscription
/// </summary>
public class PushSubscriptionResponse
{
    public long Id { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Request to update notification preferences
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public bool? NotificationsEnabled { get; set; }
    public bool? TaskAssignments { get; set; }
    public bool? TaskReminders { get; set; }
    public bool? ComplianceAlerts { get; set; }
    public bool? SyncStatus { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public string? Timezone { get; set; }
}

/// <summary>
/// Response for notification preferences
/// </summary>
public class NotificationPreferencesResponse
{
    public bool NotificationsEnabled { get; set; }
    public bool TaskAssignments { get; set; }
    public bool TaskReminders { get; set; }
    public bool ComplianceAlerts { get; set; }
    public bool SyncStatus { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public string? Timezone { get; set; }
}

#endregion

/// <summary>
/// Controller for push notification management
/// </summary>
[ApiController]
[Route("api/push")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IPushSenderService _pushSenderService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushController> _logger;

    public PushController(
        IPushNotificationService pushNotificationService,
        IPushSenderService pushSenderService,
        IConfiguration configuration,
        ILogger<PushController> logger)
    {
        _pushNotificationService = pushNotificationService;
        _pushSenderService = pushSenderService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get the VAPID public key for push subscription
    /// </summary>
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public ActionResult<object> GetVapidPublicKey()
    {
        var publicKey = _configuration["PushNotifications:VapidPublicKey"];

        if (string.IsNullOrEmpty(publicKey))
        {
            return NotFound(new { message = "Push notifications not configured" });
        }

        return Ok(new { publicKey });
    }

    /// <summary>
    /// Subscribe to push notifications
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<ActionResult<PushSubscriptionResponse>> Subscribe([FromBody] PushSubscribeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            if (string.IsNullOrEmpty(request.Endpoint) ||
                string.IsNullOrEmpty(request.P256dhKey) ||
                string.IsNullOrEmpty(request.AuthKey))
            {
                return BadRequest(new { message = "Invalid subscription data" });
            }

            var userAgent = Request.Headers.UserAgent.ToString();

            var subscription = await _pushNotificationService.SaveSubscriptionAsync(
                userId.Value,
                request.Endpoint,
                request.P256dhKey,
                request.AuthKey,
                userAgent,
                request.DeviceName);

            _logger.LogInformation(
                "User {UserId} subscribed to push notifications",
                userId.Value);

            return Ok(new PushSubscriptionResponse
            {
                Id = subscription.Id,
                DeviceName = subscription.DeviceName,
                IsActive = subscription.IsActive,
                CreatedAt = subscription.CreatedAt,
                LastUsedAt = subscription.LastUsedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to push notifications");
            return StatusCode(500, new { message = "Error subscribing to push notifications" });
        }
    }

    /// <summary>
    /// Unsubscribe from push notifications
    /// </summary>
    [HttpDelete("subscribe")]
    public async Task<ActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            if (string.IsNullOrEmpty(request.Endpoint))
            {
                return BadRequest(new { message = "Endpoint is required" });
            }

            var removed = await _pushNotificationService.RemoveSubscriptionAsync(
                userId.Value,
                request.Endpoint);

            if (!removed)
            {
                return NotFound(new { message = "Subscription not found" });
            }

            _logger.LogInformation(
                "User {UserId} unsubscribed from push notifications",
                userId.Value);

            return Ok(new { message = "Unsubscribed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from push notifications");
            return StatusCode(500, new { message = "Error unsubscribing from push notifications" });
        }
    }

    /// <summary>
    /// Remove a specific subscription by ID
    /// </summary>
    [HttpDelete("subscriptions/{id}")]
    public async Task<ActionResult> RemoveSubscription(long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            var removed = await _pushNotificationService.RemoveSubscriptionByIdAsync(
                userId.Value,
                id);

            if (!removed)
            {
                return NotFound(new { message = "Subscription not found" });
            }

            return Ok(new { message = "Subscription removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing subscription");
            return StatusCode(500, new { message = "Error removing subscription" });
        }
    }

    /// <summary>
    /// Get all active subscriptions for the current user
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<ActionResult<List<PushSubscriptionResponse>>> GetSubscriptions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            var subscriptions = await _pushNotificationService.GetUserSubscriptionsAsync(userId.Value);

            var response = subscriptions.Select(s => new PushSubscriptionResponse
            {
                Id = s.Id,
                DeviceName = s.DeviceName,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastUsedAt = s.LastUsedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, new { message = "Error getting subscriptions" });
        }
    }

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            var preferences = await _pushNotificationService.GetUserPreferencesAsync(userId.Value);

            return Ok(new NotificationPreferencesResponse
            {
                NotificationsEnabled = preferences.NotificationsEnabled,
                TaskAssignments = preferences.TaskAssignments,
                TaskReminders = preferences.TaskReminders,
                ComplianceAlerts = preferences.ComplianceAlerts,
                SyncStatus = preferences.SyncStatus,
                QuietHoursStart = preferences.QuietHoursStart?.ToString("HH:mm"),
                QuietHoursEnd = preferences.QuietHoursEnd?.ToString("HH:mm"),
                Timezone = preferences.Timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences");
            return StatusCode(500, new { message = "Error getting notification preferences" });
        }
    }

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            var currentPrefs = await _pushNotificationService.GetUserPreferencesAsync(userId.Value);

            // Apply updates
            if (request.NotificationsEnabled.HasValue)
                currentPrefs.NotificationsEnabled = request.NotificationsEnabled.Value;
            if (request.TaskAssignments.HasValue)
                currentPrefs.TaskAssignments = request.TaskAssignments.Value;
            if (request.TaskReminders.HasValue)
                currentPrefs.TaskReminders = request.TaskReminders.Value;
            if (request.ComplianceAlerts.HasValue)
                currentPrefs.ComplianceAlerts = request.ComplianceAlerts.Value;
            if (request.SyncStatus.HasValue)
                currentPrefs.SyncStatus = request.SyncStatus.Value;
            if (request.QuietHoursStart != null)
            {
                if (TimeOnly.TryParse(request.QuietHoursStart, out var start))
                    currentPrefs.QuietHoursStart = start;
                else if (string.IsNullOrEmpty(request.QuietHoursStart))
                    currentPrefs.QuietHoursStart = null;
            }
            if (request.QuietHoursEnd != null)
            {
                if (TimeOnly.TryParse(request.QuietHoursEnd, out var end))
                    currentPrefs.QuietHoursEnd = end;
                else if (string.IsNullOrEmpty(request.QuietHoursEnd))
                    currentPrefs.QuietHoursEnd = null;
            }
            if (request.Timezone != null)
                currentPrefs.Timezone = string.IsNullOrEmpty(request.Timezone) ? null : request.Timezone;

            var updated = await _pushNotificationService.UpdateUserPreferencesAsync(
                userId.Value,
                currentPrefs);

            _logger.LogInformation(
                "User {UserId} updated notification preferences",
                userId.Value);

            return Ok(new NotificationPreferencesResponse
            {
                NotificationsEnabled = updated.NotificationsEnabled,
                TaskAssignments = updated.TaskAssignments,
                TaskReminders = updated.TaskReminders,
                ComplianceAlerts = updated.ComplianceAlerts,
                SyncStatus = updated.SyncStatus,
                QuietHoursStart = updated.QuietHoursStart?.ToString("HH:mm"),
                QuietHoursEnd = updated.QuietHoursEnd?.ToString("HH:mm"),
                Timezone = updated.Timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, new { message = "Error updating notification preferences" });
        }
    }

    /// <summary>
    /// Send a test notification (development only)
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult> SendTestNotification()
    {
        try
        {
            // Only allow in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment != "Development")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Authentication required" });
            }

            var success = await _pushSenderService.SendTestNotificationAsync(userId.Value);

            if (success)
            {
                return Ok(new { message = "Test notification sent" });
            }
            else
            {
                return BadRequest(new { message = "No active subscriptions or notifications disabled" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new { message = "Error sending test notification" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
