using System.Security.Claims;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Controllers;

public class PushControllerTests
{
    private readonly Mock<IPushNotificationService> _pushNotificationServiceMock = new();
    private readonly Mock<IPushSenderService> _pushSenderServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<ILogger<PushController>> _loggerMock = new();

    private PushController CreateController(int? userId = 1)
    {
        var controller = new PushController(
            _pushNotificationServiceMock.Object,
            _pushSenderServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object
        );

        // Set up HttpContext with user claims
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim("sub", userId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
                Request = { Headers = { ["User-Agent"] = "Test Browser" } }
            }
        };

        return controller;
    }

    #region GetVapidPublicKey Tests

    [Fact]
    public void GetVapidPublicKey_WhenConfigured_ReturnsPublicKey()
    {
        // Arrange
        _configurationMock.Setup(c => c["PushNotifications:VapidPublicKey"])
            .Returns("BPTestPublicKeyAbcdefg123456789");
        var controller = CreateController();

        // Act
        var result = controller.GetVapidPublicKey();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void GetVapidPublicKey_WhenNotConfigured_ReturnsNotFound()
    {
        // Arrange
        _configurationMock.Setup(c => c["PushNotifications:VapidPublicKey"])
            .Returns((string?)null);
        var controller = CreateController();

        // Act
        var result = controller.GetVapidPublicKey();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Subscribe Tests

    [Fact]
    public async Task Subscribe_ValidRequest_ReturnsOkWithSubscription()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushSubscribeRequest
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test123",
            P256dhKey = "BPTestKey123",
            AuthKey = "TestAuth456",
            DeviceName = "Test Device"
        };

        var subscription = new PushSubscription
        {
            Id = 1,
            UserId = 1,
            Endpoint = request.Endpoint,
            P256dhKey = request.P256dhKey,
            AuthKey = request.AuthKey,
            DeviceName = request.DeviceName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _pushNotificationServiceMock
            .Setup(s => s.SaveSubscriptionAsync(1, request.Endpoint, request.P256dhKey, request.AuthKey, It.IsAny<string?>(), request.DeviceName))
            .ReturnsAsync(subscription);

        // Act
        var result = await controller.Subscribe(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PushSubscriptionResponse>(ok.Value);
        Assert.Equal(1, response.Id);
        Assert.Equal("Test Device", response.DeviceName);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task Subscribe_MissingEndpoint_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushSubscribeRequest
        {
            Endpoint = "", // Empty
            P256dhKey = "BPTestKey123",
            AuthKey = "TestAuth456"
        };

        // Act
        var result = await controller.Subscribe(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Subscribe_MissingP256dhKey_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushSubscribeRequest
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test123",
            P256dhKey = "", // Empty
            AuthKey = "TestAuth456"
        };

        // Act
        var result = await controller.Subscribe(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Subscribe_NoAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController(userId: null); // No user
        var request = new PushSubscribeRequest
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test123",
            P256dhKey = "BPTestKey123",
            AuthKey = "TestAuth456"
        };

        // Act
        var result = await controller.Subscribe(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    #endregion

    #region Unsubscribe Tests

    [Fact]
    public async Task Unsubscribe_ExistingSubscription_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushUnsubscribeRequest
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test123"
        };

        _pushNotificationServiceMock
            .Setup(s => s.RemoveSubscriptionAsync(1, request.Endpoint))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Unsubscribe(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Unsubscribe_NonExistentSubscription_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushUnsubscribeRequest
        {
            Endpoint = "https://nonexistent.endpoint"
        };

        _pushNotificationServiceMock
            .Setup(s => s.RemoveSubscriptionAsync(1, request.Endpoint))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Unsubscribe(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Unsubscribe_EmptyEndpoint_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var request = new PushUnsubscribeRequest { Endpoint = "" };

        // Act
        var result = await controller.Unsubscribe(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RemoveSubscription Tests

    [Fact]
    public async Task RemoveSubscription_ValidId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(userId: 1);

        _pushNotificationServiceMock
            .Setup(s => s.RemoveSubscriptionByIdAsync(1, 42))
            .ReturnsAsync(true);

        // Act
        var result = await controller.RemoveSubscription(42);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveSubscription_WrongUser_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 2); // Different user

        _pushNotificationServiceMock
            .Setup(s => s.RemoveSubscriptionByIdAsync(2, 42))
            .ReturnsAsync(false);

        // Act
        var result = await controller.RemoveSubscription(42);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetSubscriptions Tests

    [Fact]
    public async Task GetSubscriptions_ReturnsUserSubscriptions()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var subscriptions = new List<PushSubscription>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Endpoint = "https://endpoint1",
                DeviceName = "Phone",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                UserId = 1,
                Endpoint = "https://endpoint2",
                DeviceName = "Laptop",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await controller.GetSubscriptions();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<PushSubscriptionResponse>>(ok.Value);
        Assert.Equal(2, response.Count);
        Assert.Contains(response, r => r.DeviceName == "Phone");
        Assert.Contains(response, r => r.DeviceName == "Laptop");
    }

    [Fact]
    public async Task GetSubscriptions_NoSubscriptions_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateController(userId: 1);

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(new List<PushSubscription>());

        // Act
        var result = await controller.GetSubscriptions();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<PushSubscriptionResponse>>(ok.Value);
        Assert.Empty(response);
    }

    #endregion

    #region GetPreferences Tests

    [Fact]
    public async Task GetPreferences_ReturnsUserPreferences()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var preferences = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = false,
            TaskReminders = true,
            ComplianceAlerts = true,
            SyncStatus = false,
            QuietHoursStart = new TimeOnly(22, 0),
            QuietHoursEnd = new TimeOnly(7, 0),
            Timezone = "America/New_York"
        };

        _pushNotificationServiceMock
            .Setup(s => s.GetUserPreferencesAsync(1))
            .ReturnsAsync(preferences);

        // Act
        var result = await controller.GetPreferences();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NotificationPreferencesResponse>(ok.Value);
        Assert.True(response.NotificationsEnabled);
        Assert.False(response.TaskAssignments);
        Assert.True(response.TaskReminders);
        Assert.Equal("22:00", response.QuietHoursStart);
        Assert.Equal("07:00", response.QuietHoursEnd);
        Assert.Equal("America/New_York", response.Timezone);
    }

    #endregion

    #region UpdatePreferences Tests

    [Fact]
    public async Task UpdatePreferences_UpdatesAndReturnsPreferences()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var existingPrefs = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = true
        };

        _pushNotificationServiceMock
            .Setup(s => s.GetUserPreferencesAsync(1))
            .ReturnsAsync(existingPrefs);

        _pushNotificationServiceMock
            .Setup(s => s.UpdateUserPreferencesAsync(1, It.IsAny<NotificationPreference>()))
            .ReturnsAsync((int _, NotificationPreference p) => p);

        var request = new UpdateNotificationPreferencesRequest
        {
            NotificationsEnabled = false,
            TaskAssignments = false
        };

        // Act
        var result = await controller.UpdatePreferences(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NotificationPreferencesResponse>(ok.Value);
        Assert.False(response.NotificationsEnabled);
        Assert.False(response.TaskAssignments);
    }

    [Fact]
    public async Task UpdatePreferences_PartialUpdate_OnlyChangesSpecifiedFields()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var existingPrefs = new NotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = true,
            TaskAssignments = true,
            ComplianceAlerts = true
        };

        _pushNotificationServiceMock
            .Setup(s => s.GetUserPreferencesAsync(1))
            .ReturnsAsync(existingPrefs);

        _pushNotificationServiceMock
            .Setup(s => s.UpdateUserPreferencesAsync(1, It.IsAny<NotificationPreference>()))
            .ReturnsAsync((int _, NotificationPreference p) => p);

        var request = new UpdateNotificationPreferencesRequest
        {
            TaskAssignments = false
            // Other fields not specified - should remain unchanged
        };

        // Act
        var result = await controller.UpdatePreferences(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NotificationPreferencesResponse>(ok.Value);
        Assert.True(response.NotificationsEnabled); // Unchanged
        Assert.False(response.TaskAssignments); // Changed
        Assert.True(response.ComplianceAlerts); // Unchanged
    }

    [Fact]
    public async Task UpdatePreferences_QuietHoursUpdate_ParsesTimeCorrectly()
    {
        // Arrange
        var controller = CreateController(userId: 1);
        var existingPrefs = new NotificationPreference { UserId = 1 };

        _pushNotificationServiceMock
            .Setup(s => s.GetUserPreferencesAsync(1))
            .ReturnsAsync(existingPrefs);

        _pushNotificationServiceMock
            .Setup(s => s.UpdateUserPreferencesAsync(1, It.IsAny<NotificationPreference>()))
            .ReturnsAsync((int _, NotificationPreference p) => p);

        var request = new UpdateNotificationPreferencesRequest
        {
            QuietHoursStart = "22:00",
            QuietHoursEnd = "06:30",
            Timezone = "America/Chicago"
        };

        // Act
        var result = await controller.UpdatePreferences(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NotificationPreferencesResponse>(ok.Value);
        Assert.Equal("22:00", response.QuietHoursStart);
        Assert.Equal("06:30", response.QuietHoursEnd);
        Assert.Equal("America/Chicago", response.Timezone);
    }

    #endregion

    #region SendTestNotification Tests

    [Fact]
    public async Task SendTestNotification_InDevelopment_SendsNotification()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var controller = CreateController(userId: 1);

        _pushSenderServiceMock
            .Setup(s => s.SendTestNotificationAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await controller.SendTestNotification();

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    public async Task SendTestNotification_NoActiveSubscriptions_ReturnsBadRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var controller = CreateController(userId: 1);

        _pushSenderServiceMock
            .Setup(s => s.SendTestNotificationAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await controller.SendTestNotification();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    public async Task SendTestNotification_NotInDevelopment_ReturnsForbid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.SendTestNotification();

        // Assert
        Assert.IsType<ForbidResult>(result);

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    #endregion
}
