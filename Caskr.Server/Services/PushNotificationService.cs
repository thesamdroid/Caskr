using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caskr.Server.Services;

/// <summary>
/// Service for managing push notification subscriptions
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Save or update a push subscription for a user
    /// </summary>
    Task<PushSubscription> SaveSubscriptionAsync(
        int userId,
        string endpoint,
        string p256dhKey,
        string authKey,
        string? userAgent = null,
        string? deviceName = null);

    /// <summary>
    /// Remove a push subscription
    /// </summary>
    Task<bool> RemoveSubscriptionAsync(int userId, string endpoint);

    /// <summary>
    /// Remove a subscription by ID
    /// </summary>
    Task<bool> RemoveSubscriptionByIdAsync(int userId, long subscriptionId);

    /// <summary>
    /// Get all active subscriptions for a user
    /// </summary>
    Task<List<PushSubscription>> GetUserSubscriptionsAsync(int userId);

    /// <summary>
    /// Get user's notification preferences
    /// </summary>
    Task<NotificationPreference> GetUserPreferencesAsync(int userId);

    /// <summary>
    /// Update user's notification preferences
    /// </summary>
    Task<NotificationPreference> UpdateUserPreferencesAsync(int userId, NotificationPreference preferences);

    /// <summary>
    /// Mark a subscription as failed (after delivery failure)
    /// </summary>
    Task MarkSubscriptionFailedAsync(long subscriptionId);

    /// <summary>
    /// Mark a subscription as used successfully
    /// </summary>
    Task MarkSubscriptionUsedAsync(long subscriptionId);

    /// <summary>
    /// Deactivate a subscription (e.g., after 410 Gone response)
    /// </summary>
    Task DeactivateSubscriptionAsync(long subscriptionId);

    /// <summary>
    /// Clean up expired/invalid subscriptions
    /// </summary>
    Task<int> CleanupExpiredSubscriptionsAsync();

    /// <summary>
    /// Validate all subscriptions for a user
    /// </summary>
    Task ValidateUserSubscriptionsAsync(int userId);

    /// <summary>
    /// Check if user should receive notifications based on preferences
    /// </summary>
    Task<bool> ShouldNotifyUserAsync(int userId, NotificationType notificationType);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly CaskrDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;

    // Maximum number of consecutive failures before deactivating
    private const int MaxFailureCount = 5;

    // Days before expired subscriptions are cleaned up
    private const int ExpiredSubscriptionDays = 30;

    public PushNotificationService(CaskrDbContext context, ILogger<PushNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PushSubscription> SaveSubscriptionAsync(
        int userId,
        string endpoint,
        string p256dhKey,
        string authKey,
        string? userAgent = null,
        string? deviceName = null)
    {
        // Check if subscription already exists
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (existing != null)
        {
            // Update existing subscription
            existing.P256dhKey = p256dhKey;
            existing.AuthKey = authKey;
            existing.UserAgent = userAgent ?? existing.UserAgent;
            existing.DeviceName = deviceName ?? existing.DeviceName;
            existing.IsActive = true;
            existing.FailureCount = 0;
            existing.LastFailureAt = null;

            _logger.LogInformation(
                "Updated push subscription {SubscriptionId} for user {UserId}",
                existing.Id, userId);
        }
        else
        {
            // Create new subscription
            existing = new PushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dhKey = p256dhKey,
                AuthKey = authKey,
                UserAgent = userAgent,
                DeviceName = deviceName ?? GenerateDeviceName(userAgent),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.PushSubscriptions.Add(existing);

            _logger.LogInformation(
                "Created new push subscription for user {UserId}",
                userId);
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> RemoveSubscriptionAsync(int userId, string endpoint)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (subscription == null)
        {
            return false;
        }

        _context.PushSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Removed push subscription {SubscriptionId} for user {UserId}",
            subscription.Id, userId);

        return true;
    }

    public async Task<bool> RemoveSubscriptionByIdAsync(int userId, long subscriptionId)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

        if (subscription == null)
        {
            return false;
        }

        _context.PushSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Removed push subscription {SubscriptionId} for user {UserId}",
            subscriptionId, userId);

        return true;
    }

    public async Task<List<PushSubscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastUsedAt ?? s.CreatedAt)
            .ToListAsync();
    }

    public async Task<NotificationPreference> GetUserPreferencesAsync(int userId)
    {
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences
            preferences = new NotificationPreference
            {
                UserId = userId,
                NotificationsEnabled = true,
                TaskAssignments = true,
                TaskReminders = true,
                ComplianceAlerts = true,
                SyncStatus = true
            };

            _context.NotificationPreferences.Add(preferences);
            await _context.SaveChangesAsync();
        }

        return preferences;
    }

    public async Task<NotificationPreference> UpdateUserPreferencesAsync(
        int userId,
        NotificationPreference preferences)
    {
        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (existing != null)
        {
            existing.NotificationsEnabled = preferences.NotificationsEnabled;
            existing.TaskAssignments = preferences.TaskAssignments;
            existing.TaskReminders = preferences.TaskReminders;
            existing.ComplianceAlerts = preferences.ComplianceAlerts;
            existing.SyncStatus = preferences.SyncStatus;
            existing.QuietHoursStart = preferences.QuietHoursStart;
            existing.QuietHoursEnd = preferences.QuietHoursEnd;
            existing.Timezone = preferences.Timezone;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            preferences.UserId = userId;
            preferences.UpdatedAt = DateTime.UtcNow;
            _context.NotificationPreferences.Add(preferences);
            existing = preferences;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated notification preferences for user {UserId}",
            userId);

        return existing;
    }

    public async Task MarkSubscriptionFailedAsync(long subscriptionId)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(subscriptionId);
        if (subscription == null) return;

        subscription.FailureCount++;
        subscription.LastFailureAt = DateTime.UtcNow;

        // Deactivate if too many failures
        if (subscription.FailureCount >= MaxFailureCount)
        {
            subscription.IsActive = false;
            _logger.LogWarning(
                "Deactivated push subscription {SubscriptionId} after {FailureCount} failures",
                subscriptionId, subscription.FailureCount);
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkSubscriptionUsedAsync(long subscriptionId)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(subscriptionId);
        if (subscription == null) return;

        subscription.LastUsedAt = DateTime.UtcNow;
        subscription.FailureCount = 0;
        subscription.LastFailureAt = null;

        await _context.SaveChangesAsync();
    }

    public async Task DeactivateSubscriptionAsync(long subscriptionId)
    {
        var subscription = await _context.PushSubscriptions.FindAsync(subscriptionId);
        if (subscription == null) return;

        subscription.IsActive = false;

        _logger.LogInformation(
            "Deactivated push subscription {SubscriptionId}",
            subscriptionId);

        await _context.SaveChangesAsync();
    }

    public async Task<int> CleanupExpiredSubscriptionsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-ExpiredSubscriptionDays);

        var expiredSubscriptions = await _context.PushSubscriptions
            .Where(s => !s.IsActive &&
                        (s.LastUsedAt ?? s.CreatedAt) < cutoffDate)
            .ToListAsync();

        if (expiredSubscriptions.Count > 0)
        {
            _context.PushSubscriptions.RemoveRange(expiredSubscriptions);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned up {Count} expired push subscriptions",
                expiredSubscriptions.Count);
        }

        return expiredSubscriptions.Count;
    }

    public async Task ValidateUserSubscriptionsAsync(int userId)
    {
        // This would ideally send a test notification to each subscription
        // and deactivate any that fail
        var subscriptions = await GetUserSubscriptionsAsync(userId);

        _logger.LogInformation(
            "Validating {Count} subscriptions for user {UserId}",
            subscriptions.Count, userId);

        // Actual validation would be done by the PushSenderService
    }

    public async Task<bool> ShouldNotifyUserAsync(int userId, NotificationType notificationType)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        // Check master toggle
        if (!preferences.NotificationsEnabled)
        {
            return false;
        }

        // Check category-specific toggles
        var shouldNotify = notificationType switch
        {
            NotificationType.TaskAssigned => preferences.TaskAssignments,
            NotificationType.TaskDueSoon => preferences.TaskReminders,
            NotificationType.TaskUrgent => preferences.TaskAssignments,
            NotificationType.ComplianceReportDue => preferences.ComplianceAlerts,
            NotificationType.ComplianceRequiresApproval => preferences.ComplianceAlerts,
            NotificationType.ComplianceApproved => preferences.ComplianceAlerts,
            NotificationType.ComplianceRejected => preferences.ComplianceAlerts,
            NotificationType.SyncCompleted => preferences.SyncStatus,
            NotificationType.SyncFailed => preferences.SyncStatus,
            NotificationType.General => true,
            _ => true
        };

        if (!shouldNotify)
        {
            return false;
        }

        // Check quiet hours
        if (preferences.QuietHoursStart.HasValue && preferences.QuietHoursEnd.HasValue)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(
                preferences.Timezone ?? "UTC");
            var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
            var currentTime = TimeOnly.FromDateTime(userLocalTime);

            var start = preferences.QuietHoursStart.Value;
            var end = preferences.QuietHoursEnd.Value;

            // Handle overnight quiet hours (e.g., 22:00 - 08:00)
            if (start > end)
            {
                // Quiet hours span midnight
                if (currentTime >= start || currentTime <= end)
                {
                    _logger.LogDebug(
                        "Suppressing notification for user {UserId} during quiet hours",
                        userId);
                    return false;
                }
            }
            else
            {
                // Same day quiet hours
                if (currentTime >= start && currentTime <= end)
                {
                    _logger.LogDebug(
                        "Suppressing notification for user {UserId} during quiet hours",
                        userId);
                    return false;
                }
            }
        }

        return true;
    }

    private static string GenerateDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return "Unknown Device";
        }

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("iphone"))
            return "iPhone";
        if (ua.Contains("ipad"))
            return "iPad";
        if (ua.Contains("android"))
            return "Android Device";
        if (ua.Contains("windows"))
            return "Windows Device";
        if (ua.Contains("mac"))
            return "Mac";
        if (ua.Contains("linux"))
            return "Linux Device";

        return "Unknown Device";
    }
}
