using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Services;

public class PushNotificationServiceTests
{
    private readonly Mock<ILogger<PushNotificationService>> _loggerMock = new();

    private CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private PushNotificationService CreateService(CaskrDbContext context)
    {
        return new PushNotificationService(context, _loggerMock.Object);
    }

    #region Subscription Tests

    [Fact]
    public async Task SaveSubscriptionAsync_NewSubscription_CreatesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.SaveSubscriptionAsync(
            userId: 1,
            endpoint: "https://fcm.googleapis.com/fcm/send/test123",
            p256dhKey: "BPtestkey123456789",
            authKey: "testauthkey123"
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("https://fcm.googleapis.com/fcm/send/test123", result.Endpoint);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task SaveSubscriptionAsync_ExistingSubscription_UpdatesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingSubscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test123",
            P256dhKey = "oldkey",
            AuthKey = "oldauth",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.PushSubscriptions.Add(existingSubscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SaveSubscriptionAsync(
            userId: 1,
            endpoint: "https://fcm.googleapis.com/fcm/send/test123",
            p256dhKey: "newkey",
            authKey: "newauth"
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newkey", result.P256dhKey);
        Assert.Equal("newauth", result.AuthKey);

        // Should still be only one subscription
        var count = await context.PushSubscriptions.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveSubscriptionAsync_ExistingSubscription_RemovesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test.endpoint/123",
            P256dhKey = "key",
            AuthKey = "auth",
            IsActive = true
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.RemoveSubscriptionAsync(1, "https://test.endpoint/123");

        // Assert
        Assert.True(result);
        var count = await context.PushSubscriptions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RemoveSubscriptionAsync_NonExistentSubscription_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.RemoveSubscriptionAsync(1, "https://nonexistent.endpoint");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveSubscriptionByIdAsync_WrongUser_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test.endpoint/123",
            P256dhKey = "key",
            AuthKey = "auth"
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act - Try to delete with wrong user ID
        var result = await service.RemoveSubscriptionByIdAsync(userId: 2, subscriptionId: subscription.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserSubscriptionsAsync_ReturnsOnlyActiveSubscriptions()
    {
        // Arrange
        using var context = CreateDbContext();
        context.PushSubscriptions.AddRange(
            new PushSubscription { UserId = 1, Endpoint = "https://active1", P256dhKey = "k", AuthKey = "a", IsActive = true },
            new PushSubscription { UserId = 1, Endpoint = "https://active2", P256dhKey = "k", AuthKey = "a", IsActive = true },
            new PushSubscription { UserId = 1, Endpoint = "https://inactive", P256dhKey = "k", AuthKey = "a", IsActive = false },
            new PushSubscription { UserId = 2, Endpoint = "https://other", P256dhKey = "k", AuthKey = "a", IsActive = true }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetUserSubscriptionsAsync(1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.True(s.IsActive));
        Assert.All(result, s => Assert.Equal(1, s.UserId));
    }

    #endregion

    #region Preferences Tests

    [Fact]
    public async Task GetUserPreferencesAsync_NewUser_CreatesDefaultPreferences()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetUserPreferencesAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.True(result.NotificationsEnabled);
        Assert.True(result.TaskAssignments);
        Assert.True(result.TaskReminders);
        Assert.True(result.ComplianceAlerts);
        Assert.True(result.SyncStatus);
    }

    [Fact]
    public async Task GetUserPreferencesAsync_ExistingUser_ReturnsStoredPreferences()
    {
        // Arrange
        using var context = CreateDbContext();
        var preferences = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = false,
            TaskReminders = true,
            ComplianceAlerts = true,
            SyncStatus = false
        };
        context.NotificationPreferences.Add(preferences);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetUserPreferencesAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.TaskAssignments);
        Assert.True(result.TaskReminders);
        Assert.False(result.SyncStatus);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_UpdatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var existingPrefs = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = true
        };
        context.NotificationPreferences.Add(existingPrefs);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var updatedPrefs = new NotificationPreference
        {
            NotificationsEnabled = false,
            TaskAssignments = false
        };
        var result = await service.UpdateUserPreferencesAsync(1, updatedPrefs);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NotificationsEnabled);
        Assert.False(result.TaskAssignments);
    }

    #endregion

    #region Failure Handling Tests

    [Fact]
    public async Task MarkSubscriptionFailedAsync_IncrementsFailureCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test",
            P256dhKey = "k",
            AuthKey = "a",
            FailureCount = 0,
            IsActive = true
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.MarkSubscriptionFailedAsync(subscription.Id);

        // Assert
        await context.Entry(subscription).ReloadAsync();
        Assert.Equal(1, subscription.FailureCount);
        Assert.NotNull(subscription.LastFailureAt);
        Assert.True(subscription.IsActive); // Not yet deactivated
    }

    [Fact]
    public async Task MarkSubscriptionFailedAsync_FiveFailures_DeactivatesSubscription()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test",
            P256dhKey = "k",
            AuthKey = "a",
            FailureCount = 4, // One more will reach 5
            IsActive = true
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.MarkSubscriptionFailedAsync(subscription.Id);

        // Assert
        await context.Entry(subscription).ReloadAsync();
        Assert.Equal(5, subscription.FailureCount);
        Assert.False(subscription.IsActive);
    }

    [Fact]
    public async Task MarkSubscriptionUsedAsync_ResetsFailureCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test",
            P256dhKey = "k",
            AuthKey = "a",
            FailureCount = 3,
            LastFailureAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.MarkSubscriptionUsedAsync(subscription.Id);

        // Assert
        await context.Entry(subscription).ReloadAsync();
        Assert.Equal(0, subscription.FailureCount);
        Assert.Null(subscription.LastFailureAt);
        Assert.NotNull(subscription.LastUsedAt);
    }

    [Fact]
    public async Task DeactivateSubscriptionAsync_DeactivatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var subscription = new PushSubscription
        {
            UserId = 1,
            Endpoint = "https://test",
            P256dhKey = "k",
            AuthKey = "a",
            IsActive = true
        };
        context.PushSubscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.DeactivateSubscriptionAsync(subscription.Id);

        // Assert
        await context.Entry(subscription).ReloadAsync();
        Assert.False(subscription.IsActive);
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task CleanupExpiredSubscriptionsAsync_RemovesOldInactiveSubscriptions()
    {
        // Arrange
        using var context = CreateDbContext();
        context.PushSubscriptions.AddRange(
            // Old inactive - should be removed
            new PushSubscription
            {
                UserId = 1,
                Endpoint = "https://old-inactive",
                P256dhKey = "k",
                AuthKey = "a",
                IsActive = false,
                LastUsedAt = DateTime.UtcNow.AddDays(-60)
            },
            // Recent inactive - should not be removed
            new PushSubscription
            {
                UserId = 1,
                Endpoint = "https://recent-inactive",
                P256dhKey = "k",
                AuthKey = "a",
                IsActive = false,
                LastUsedAt = DateTime.UtcNow.AddDays(-7)
            },
            // Active - should not be removed
            new PushSubscription
            {
                UserId = 1,
                Endpoint = "https://active",
                P256dhKey = "k",
                AuthKey = "a",
                IsActive = true
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var removedCount = await service.CleanupExpiredSubscriptionsAsync();

        // Assert
        Assert.Equal(1, removedCount);
        var remainingCount = await context.PushSubscriptions.CountAsync();
        Assert.Equal(2, remainingCount);
    }

    #endregion

    #region Notification Preference Check Tests

    [Fact]
    public async Task ShouldNotifyUserAsync_NotificationsDisabled_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var prefs = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = false,
            TaskAssignments = true
        };
        context.NotificationPreferences.Add(prefs);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldNotifyUserAsync_CategoryDisabled_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var prefs = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = false,
            ComplianceAlerts = true
        };
        context.NotificationPreferences.Add(prefs);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var taskResult = await service.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned);
        var complianceResult = await service.ShouldNotifyUserAsync(1, NotificationType.ComplianceReportDue);

        // Assert
        Assert.False(taskResult);
        Assert.True(complianceResult);
    }

    [Fact]
    public async Task ShouldNotifyUserAsync_AllEnabled_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act - Will create default (all enabled) preferences
        var result = await service.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned);

        // Assert
        Assert.True(result);
    }

    #endregion
}
