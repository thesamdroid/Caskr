using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests;

public class WebhookServiceTests
{
    private readonly CaskrDbContext _dbContext;
    private readonly Mock<IBackgroundTaskQueue> _taskQueue = new();
    private readonly Mock<ILogger<WebhookService>> _logger = new();
    private readonly WebhookService _service;

    public WebhookServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CaskrDbContext(options);
        _taskQueue.Setup(t => t.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()))
            .Returns(ValueTask.CompletedTask);

        _service = new WebhookService(_dbContext, _taskQueue.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ValidRequest_CreatesSubscription()
    {
        // Arrange
        var company = new Company { Id = 1, CompanyName = "Test Co" };
        var user = new User { Id = 1, CompanyId = 1, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new WebhookSubscriptionRequest
        {
            Name = "Test Webhook",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { WebhookEventTypes.OrderCreated, WebhookEventTypes.BarrelCreated }
        };

        // Act
        var subscription = await _service.CreateSubscriptionAsync(1, request, 1);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal("Test Webhook", subscription.Name);
        Assert.Equal("https://example.com/webhook", subscription.TargetUrl);
        Assert.Equal(2, subscription.EventTypes.Count);
        Assert.Contains(WebhookEventTypes.OrderCreated, subscription.EventTypes);
        Assert.Contains(WebhookEventTypes.BarrelCreated, subscription.EventTypes);
        Assert.True(subscription.IsActive);
        Assert.NotEmpty(subscription.SecretKey);
        Assert.Equal(64, subscription.SecretKey.Length); // 32 bytes = 64 hex chars
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidEventType_ThrowsArgumentException()
    {
        // Arrange
        var request = new WebhookSubscriptionRequest
        {
            Name = "Test Webhook",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { "invalid.event.type" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSubscriptionAsync(1, request, 1));

        Assert.Contains("Invalid event types", exception.Message);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var request = new WebhookSubscriptionRequest
        {
            Name = "Test Webhook",
            TargetUrl = "not-a-valid-url",
            EventTypes = new List<string> { WebhookEventTypes.OrderCreated }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSubscriptionAsync(1, request, 1));

        Assert.Contains("valid HTTP or HTTPS URL", exception.Message);
    }

    [Fact]
    public async Task TriggerEventAsync_WithMatchingSubscription_CreatesDelivery()
    {
        // Arrange
        var company = new Company { Id = 2, CompanyName = "Test Co 2" };
        var user = new User { Id = 2, CompanyId = 2, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var subscription = new WebhookSubscription
        {
            CompanyId = 2,
            Name = "Test Subscription",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { WebhookEventTypes.OrderCreated },
            IsActive = true,
            SecretKey = "testsecret1234567890",
            CreatedByUserId = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.TriggerEventAsync(WebhookEventTypes.OrderCreated, 123, new { id = 123 }, 2);

        // Assert
        var deliveries = await _dbContext.WebhookDeliveries.ToListAsync();
        Assert.Single(deliveries);
        Assert.Equal(WebhookEventTypes.OrderCreated, deliveries[0].EventType);
        Assert.Equal(123, deliveries[0].EventId);
        Assert.Equal(WebhookDeliveryStatus.Pending, deliveries[0].DeliveryStatus);
    }

    [Fact]
    public async Task TriggerEventAsync_NoMatchingSubscription_CreatesNoDelivery()
    {
        // Arrange
        var company = new Company { Id = 3, CompanyName = "Test Co 3" };
        var user = new User { Id = 3, CompanyId = 3, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var subscription = new WebhookSubscription
        {
            CompanyId = 3,
            Name = "Test Subscription",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { WebhookEventTypes.BarrelCreated }, // Different event type
            IsActive = true,
            SecretKey = "testsecret1234567890",
            CreatedByUserId = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.TriggerEventAsync(WebhookEventTypes.OrderCreated, 456, new { id = 456 }, 3);

        // Assert
        var deliveries = await _dbContext.WebhookDeliveries.ToListAsync();
        Assert.Empty(deliveries);
    }

    [Fact]
    public async Task TriggerEventAsync_InactiveSubscription_CreatesNoDelivery()
    {
        // Arrange
        var company = new Company { Id = 4, CompanyName = "Test Co 4" };
        var user = new User { Id = 4, CompanyId = 4, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var subscription = new WebhookSubscription
        {
            CompanyId = 4,
            Name = "Test Subscription",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { WebhookEventTypes.OrderCreated },
            IsActive = false, // Inactive
            SecretKey = "testsecret1234567890",
            CreatedByUserId = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.TriggerEventAsync(WebhookEventTypes.OrderCreated, 789, new { id = 789 }, 4);

        // Assert
        var deliveries = await _dbContext.WebhookDeliveries.ToListAsync();
        Assert.Empty(deliveries);
    }

    [Fact]
    public async Task DeactivateSubscriptionAsync_ExistingSubscription_DeactivatesIt()
    {
        // Arrange
        var company = new Company { Id = 5, CompanyName = "Test Co 5" };
        var user = new User { Id = 5, CompanyId = 5, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var subscription = new WebhookSubscription
        {
            CompanyId = 5,
            Name = "Test Subscription",
            TargetUrl = "https://example.com/webhook",
            EventTypes = new List<string> { WebhookEventTypes.OrderCreated },
            IsActive = true,
            SecretKey = "testsecret1234567890",
            CreatedByUserId = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeactivateSubscriptionAsync(subscription.Id);

        // Assert
        var updated = await _dbContext.WebhookSubscriptions.FindAsync(subscription.Id);
        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task DeactivateSubscriptionAsync_NonExistentSubscription_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeactivateSubscriptionAsync(99999));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ReturnsCompanySubscriptions()
    {
        // Arrange
        var company = new Company { Id = 6, CompanyName = "Test Co 6" };
        var user = new User { Id = 6, CompanyId = 6, Name = "Test User", Email = "test@example.com", UserTypeId = 1 };
        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _dbContext.WebhookSubscriptions.AddRange(
            new WebhookSubscription
            {
                CompanyId = 6,
                Name = "Subscription 1",
                TargetUrl = "https://example.com/webhook1",
                EventTypes = new List<string> { WebhookEventTypes.OrderCreated },
                IsActive = true,
                SecretKey = "secret1",
                CreatedByUserId = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new WebhookSubscription
            {
                CompanyId = 6,
                Name = "Subscription 2",
                TargetUrl = "https://example.com/webhook2",
                EventTypes = new List<string> { WebhookEventTypes.BarrelCreated },
                IsActive = true,
                SecretKey = "secret2",
                CreatedByUserId = 6,
                CreatedAt = DateTime.UtcNow.AddMinutes(1),
                UpdatedAt = DateTime.UtcNow
            },
            new WebhookSubscription
            {
                CompanyId = 999, // Different company
                Name = "Other Subscription",
                TargetUrl = "https://example.com/other",
                EventTypes = new List<string> { WebhookEventTypes.OrderCreated },
                IsActive = true,
                SecretKey = "secret3",
                CreatedByUserId = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var subscriptions = (await _service.GetSubscriptionsAsync(6)).ToList();

        // Assert
        Assert.Equal(2, subscriptions.Count);
        Assert.All(subscriptions, s => Assert.Equal(6, s.CompanyId));
    }

    [Fact]
    public void CalculateSignature_ProducesConsistentSignature()
    {
        // Arrange
        var payload = "{\"event_type\":\"order.created\",\"event_id\":123}";
        var secretKey = "testsecretkey";

        // Act
        var signature1 = WebhookService.CalculateSignature(payload, secretKey);
        var signature2 = WebhookService.CalculateSignature(payload, secretKey);

        // Assert
        Assert.Equal(signature1, signature2);
        Assert.StartsWith("sha256=", signature1);
    }

    [Fact]
    public void CalculateSignature_DifferentPayload_ProducesDifferentSignature()
    {
        // Arrange
        var payload1 = "{\"event_id\":1}";
        var payload2 = "{\"event_id\":2}";
        var secretKey = "testsecretkey";

        // Act
        var signature1 = WebhookService.CalculateSignature(payload1, secretKey);
        var signature2 = WebhookService.CalculateSignature(payload2, secretKey);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public async Task TriggerEventAsync_InvalidEventType_DoesNotCreateDelivery()
    {
        // Act
        await _service.TriggerEventAsync("invalid.event.type", 123, new { id = 123 }, 1);

        // Assert
        var deliveries = await _dbContext.WebhookDeliveries.ToListAsync();
        Assert.Empty(deliveries);
    }

    [Fact]
    public void WebhookEventTypes_AllEventTypes_ContainsAllDefinedEvents()
    {
        // Assert all event types are in the array
        Assert.Contains(WebhookEventTypes.BarrelCreated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.BarrelUpdated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.BarrelDeleted, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.BarrelMoved, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.BatchCreated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.BatchCompleted, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.OrderCreated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.OrderCompleted, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.TaskCreated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.TaskCompleted, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.TransferCreated, WebhookEventTypes.AllEventTypes);
        Assert.Contains(WebhookEventTypes.TtbReportSubmitted, WebhookEventTypes.AllEventTypes);
    }

    [Fact]
    public void WebhookEventTypes_IsValidEventType_ReturnsTrueForValidTypes()
    {
        Assert.True(WebhookEventTypes.IsValidEventType(WebhookEventTypes.OrderCreated));
        Assert.True(WebhookEventTypes.IsValidEventType(WebhookEventTypes.BarrelCreated));
        Assert.True(WebhookEventTypes.IsValidEventType(WebhookEventTypes.TtbReportSubmitted));
    }

    [Fact]
    public void WebhookEventTypes_IsValidEventType_ReturnsFalseForInvalidTypes()
    {
        Assert.False(WebhookEventTypes.IsValidEventType("invalid.event"));
        Assert.False(WebhookEventTypes.IsValidEventType(""));
        Assert.False(WebhookEventTypes.IsValidEventType("order.unknown"));
    }
}
