using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Caskr.Server.Tests.Services;

public class PushSenderServiceTests
{
    private Mock<IPushNotificationService> _pushNotificationServiceMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private Mock<ILogger<PushSenderService>> _loggerMock = null!;
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<HttpMessageHandler> _httpHandlerMock = null!;

    private CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private void ResetMocks()
    {
        _pushNotificationServiceMock = new Mock<IPushNotificationService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<PushSenderService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
    }

    private PushSenderService CreateService(CaskrDbContext context, HttpClient? httpClient = null)
    {
        ResetMocks();

        // Setup configuration
        _configurationMock.Setup(c => c["PushNotifications:VapidSubject"])
            .Returns("mailto:admin@caskr.co");
        _configurationMock.Setup(c => c["PushNotifications:VapidPublicKey"])
            .Returns("BPTestPublicKeyABCDEF123456789012345678901234567890");
        _configurationMock.Setup(c => c["PushNotifications:VapidPrivateKey"])
            .Returns("TestPrivateKey123456789012345678901234567890");

        // Setup HTTP client
        var client = httpClient ?? new HttpClient(_httpHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("PushService")).Returns(client);

        return new TestablePushSenderService(
            context,
            _pushNotificationServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object
        );
    }

    /// <summary>
    /// Test double that bypasses actual WebPush encryption for unit testing.
    /// </summary>
    private class TestablePushSenderService : PushSenderService
    {
        public TestablePushSenderService(
            CaskrDbContext context,
            IPushNotificationService pushNotificationService,
            IConfiguration configuration,
            ILogger<PushSenderService> logger,
            IHttpClientFactory httpClientFactory)
            : base(context, pushNotificationService, configuration, logger, httpClientFactory)
        {
        }

        /// <summary>
        /// Returns a simple test payload instead of actual encrypted data.
        /// </summary>
        protected override byte[] EncryptPayload(byte[] payload, string p256dhKey, string authKey)
        {
            // Return a simple test payload for unit testing
            return new byte[] { 0x01, 0x02, 0x03, 0x04 };
        }

        /// <summary>
        /// Returns simple test headers instead of actual VAPID headers.
        /// </summary>
        protected override Dictionary<string, string> CreateVapidHeaders(string endpoint)
        {
            return new Dictionary<string, string>
            {
                { "Authorization", "vapid t=test-token, k=test-key" }
            };
        }
    }

    #region SendToUserAsync Tests

    [Fact]
    public async Task SendToUserAsync_UserHasNoSubscriptions_ReturnsZero()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned))
            .ReturnsAsync(true);

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(new List<PushSubscription>());

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test notification",
            Type = NotificationType.TaskAssigned
        };

        // Act
        var result = await service.SendToUserAsync(1, payload);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SendToUserAsync_UserPreferencesDisabled_ReturnsZero()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned))
            .ReturnsAsync(false);

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test notification",
            Type = NotificationType.TaskAssigned
        };

        // Act
        var result = await service.SendToUserAsync(1, payload);

        // Assert
        Assert.Equal(0, result);

        // Verify subscriptions were never fetched since preferences blocked the notification
        _pushNotificationServiceMock.Verify(
            s => s.GetUserSubscriptionsAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_MultipleSubscriptions_SendsToAll()
    {
        // Arrange
        using var context = CreateDbContext();

        var subscriptions = new List<PushSubscription>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Endpoint = "https://fcm.googleapis.com/fcm/send/1",
                P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
            },
            new()
            {
                Id = 2,
                UserId = 1,
                Endpoint = "https://fcm.googleapis.com/fcm/send/2",
                P256dhKey = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                AuthKey = "BBBBBBBBBBBBBBBBBBBBBB"
            }
        };

        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned))
            .ReturnsAsync(true);

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(subscriptions);

        // Setup HTTP handler to return success
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test notification",
            Type = NotificationType.TaskAssigned
        };

        // Act
        var result = await service.SendToUserAsync(1, payload);

        // Assert
        Assert.Equal(2, result);
    }

    #endregion

    #region SendToUsersAsync Tests

    [Fact]
    public async Task SendToUsersAsync_MultipleUsers_SendsToAll()
    {
        // Arrange
        using var context = CreateDbContext();

        var userIds = new[] { 1, 2, 3 };

        foreach (var userId in userIds)
        {
            _pushNotificationServiceMock
                .Setup(s => s.ShouldNotifyUserAsync(userId, NotificationType.General))
                .ReturnsAsync(true);

            _pushNotificationServiceMock
                .Setup(s => s.GetUserSubscriptionsAsync(userId))
                .ReturnsAsync(new List<PushSubscription>
                {
                    new()
                    {
                        Id = userId,
                        UserId = userId,
                        Endpoint = $"https://fcm.googleapis.com/fcm/send/{userId}",
                        P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                        AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
                    }
                });
        }

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        var payload = new PushNotificationPayload
        {
            Title = "Broadcast",
            Body = "Message to all users",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToUsersAsync(userIds, payload);

        // Assert
        Assert.Equal(3, result);
    }

    #endregion

    #region SendToSubscriptionAsync Tests

    [Fact]
    public async Task SendToSubscriptionAsync_NoVapidKey_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();

        // Setup configuration without VAPID private key
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["PushNotifications:VapidSubject"])
            .Returns("mailto:admin@caskr.co");
        configMock.Setup(c => c["PushNotifications:VapidPublicKey"])
            .Returns("BPTestPublicKey");
        configMock.Setup(c => c["PushNotifications:VapidPrivateKey"])
            .Returns((string?)null);

        var httpClient = new HttpClient(_httpHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("PushService")).Returns(httpClient);

        var service = new PushSenderService(
            context,
            _pushNotificationServiceMock.Object,
            configMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object
        );

        var subscription = new PushSubscription
        {
            Id = 1,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "BPTestKey",
            AuthKey = "TestAuth"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendToSubscriptionAsync_SuccessfulSend_MarksUsed()
    {
        // Arrange
        using var context = CreateDbContext();

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 42,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.True(result);
        _pushNotificationServiceMock.Verify(
            s => s.MarkSubscriptionUsedAsync(42),
            Times.Once);
    }

    [Fact]
    public async Task SendToSubscriptionAsync_410Gone_DeactivatesSubscription()
    {
        // Arrange
        using var context = CreateDbContext();

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Gone));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 42,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.False(result);
        _pushNotificationServiceMock.Verify(
            s => s.DeactivateSubscriptionAsync(42),
            Times.Once);
    }

    [Fact]
    public async Task SendToSubscriptionAsync_ServerError_MarksFailure()
    {
        // Arrange
        using var context = CreateDbContext();

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 42,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.False(result);
        _pushNotificationServiceMock.Verify(
            s => s.MarkSubscriptionFailedAsync(42),
            Times.Once);
    }

    [Fact]
    public async Task SendToSubscriptionAsync_Exception_MarksFailure()
    {
        // Arrange
        using var context = CreateDbContext();

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 42,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        var result = await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.False(result);
        _pushNotificationServiceMock.Verify(
            s => s.MarkSubscriptionFailedAsync(42),
            Times.Once);
    }

    #endregion

    #region SendTestNotificationAsync Tests

    [Fact]
    public async Task SendTestNotificationAsync_SuccessfulSend_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();

        var subscriptions = new List<PushSubscription>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Endpoint = "https://fcm.googleapis.com/fcm/send/test",
                P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
            }
        };

        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.General))
            .ReturnsAsync(true);

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(subscriptions);

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        // Act
        var result = await service.SendTestNotificationAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendTestNotificationAsync_NoSubscriptions_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();

        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.General))
            .ReturnsAsync(true);

        _pushNotificationServiceMock
            .Setup(s => s.GetUserSubscriptionsAsync(1))
            .ReturnsAsync(new List<PushSubscription>());

        var service = CreateService(context);

        // Act
        var result = await service.SendTestNotificationAsync(1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Payload Serialization Tests

    [Fact]
    public async Task SendToSubscriptionAsync_IncludesAllPayloadFields()
    {
        // Arrange
        using var context = CreateDbContext();
        HttpRequestMessage? capturedRequest = null;

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 1,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Task Assigned",
            Body = "You have been assigned a new task",
            Icon = "/icons/task.png",
            Badge = "/icons/badge.png",
            Tag = "task-123",
            Type = NotificationType.TaskAssigned,
            EntityId = 123,
            Url = "/tasks/123",
            Data = new Dictionary<string, string> { { "priority", "high" } },
            Actions = new List<NotificationAction>
            {
                new() { Action = "view", Title = "View Task", Icon = "/icons/view.png" },
                new() { Action = "dismiss", Title = "Dismiss" }
            }
        };

        // Act
        await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://fcm.googleapis.com/fcm/send/test", capturedRequest.RequestUri?.ToString());
        Assert.Contains("Authorization", capturedRequest.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task SendToSubscriptionAsync_SetsCorrectHeaders()
    {
        // Arrange
        using var context = CreateDbContext();
        HttpRequestMessage? capturedRequest = null;

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = CreateService(context);

        var subscription = new PushSubscription
        {
            Id = 1,
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            P256dhKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            AuthKey = "AAAAAAAAAAAAAAAAAAAAAA"
        };

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test",
            Type = NotificationType.General
        };

        // Act
        await service.SendToSubscriptionAsync(subscription, payload);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("TTL"));
        Assert.True(capturedRequest.Headers.Contains("Urgency"));
        Assert.Equal("86400", capturedRequest.Headers.GetValues("TTL").First());
        Assert.Equal("normal", capturedRequest.Headers.GetValues("Urgency").First());
    }

    #endregion

    #region Quiet Hours Tests

    [Fact]
    public async Task SendToUserAsync_DuringQuietHours_ReturnsZero()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Service should check preferences which include quiet hours
        _pushNotificationServiceMock
            .Setup(s => s.ShouldNotifyUserAsync(1, NotificationType.TaskAssigned))
            .ReturnsAsync(false); // Quiet hours in effect

        var payload = new PushNotificationPayload
        {
            Title = "Test",
            Body = "Test notification",
            Type = NotificationType.TaskAssigned
        };

        // Act
        var result = await service.SendToUserAsync(1, payload);

        // Assert
        Assert.Equal(0, result);
        _pushNotificationServiceMock.Verify(
            s => s.GetUserSubscriptionsAsync(It.IsAny<int>()),
            Times.Never);
    }

    #endregion
}
