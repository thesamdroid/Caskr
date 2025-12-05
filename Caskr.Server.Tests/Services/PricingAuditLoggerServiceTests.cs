using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Caskr.server.Services.Pricing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Services;

/// <summary>
/// Tests for the PricingAuditLoggerService which logs all pricing admin changes.
/// </summary>
public class PricingAuditLoggerServiceTests : IDisposable
{
    private readonly CaskrDbContext _context;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<PricingAuditLoggerService>> _loggerMock;
    private readonly PricingAuditLoggerService _service;

    public PricingAuditLoggerServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<PricingAuditLoggerService>>();

        SetupHttpContext();

        _service = new PricingAuditLoggerService(
            _context,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);
    }

    private void SetupHttpContext(string? ipAddress = "127.0.0.1", string? userAgent = "TestAgent/1.0")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = ipAddress != null
            ? System.Net.IPAddress.Parse(ipAddress)
            : null;

        if (userAgent != null)
        {
            httpContext.Request.Headers["User-Agent"] = userAgent;
        }

        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region LogChangeAsync Tests

    [Fact]
    public async Task LogChangeAsync_CreatesPricingTierLogEntry()
    {
        var tier = new PricingTier
        {
            Id = 1,
            Name = "Craft",
            Slug = "craft",
            MonthlyPriceCents = 29900,
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("PricingTier", log.EntityType);
        Assert.Equal(1, log.EntityId);
        Assert.Equal(PricingAuditAction.Create, log.Action);
        Assert.Equal(1, log.ChangedByUserId);
        Assert.Null(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("Craft", log.NewValues);
    }

    [Fact]
    public async Task LogChangeAsync_CreatesPricingFeatureLogEntry()
    {
        var feature = new PricingFeature
        {
            Id = 1,
            Name = "TTB Compliance",
            Description = "Automated compliance",
            Category = "Compliance",
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, feature, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("PricingFeature", log.EntityType);
        Assert.Contains("TTB Compliance", log.NewValues);
    }

    [Fact]
    public async Task LogChangeAsync_CreatesPricingFaqLogEntry()
    {
        var faq = new PricingFaq
        {
            Id = 1,
            Question = "What is included?",
            Answer = "Everything.",
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, faq, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("PricingFaq", log.EntityType);
    }

    [Fact]
    public async Task LogChangeAsync_CreatesPricingPromotionLogEntry()
    {
        var promo = new PricingPromotion
        {
            Id = 1,
            Code = "WELCOME20",
            Description = "20% off",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20,
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, promo, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("PricingPromotion", log.EntityType);
        Assert.Contains("WELCOME20", log.NewValues);
    }

    [Fact]
    public async Task LogChangeAsync_CreatesPricingTierFeatureLogEntry()
    {
        var tierFeature = new PricingTierFeature
        {
            Id = 1,
            TierId = 1,
            FeatureId = 1,
            IsIncluded = true,
            LimitValue = "Unlimited"
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, tierFeature, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("PricingTierFeature", log.EntityType);
    }

    [Fact]
    public async Task LogChangeAsync_RecordsOldValuesOnUpdate()
    {
        var oldTier = new PricingTier
        {
            Id = 1,
            Name = "Old Name",
            Slug = "old-slug",
            MonthlyPriceCents = 19900,
            IsActive = true
        };

        var newTier = new PricingTier
        {
            Id = 1,
            Name = "New Name",
            Slug = "new-slug",
            MonthlyPriceCents = 29900,
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Update, newTier, oldTier, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(PricingAuditAction.Update, log.Action);
        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("Old Name", log.OldValues);
        Assert.Contains("New Name", log.NewValues);
    }

    [Fact]
    public async Task LogChangeAsync_UsesOldEntityIdOnDelete()
    {
        var tier = new PricingTier
        {
            Id = 5,
            Name = "Deleted Tier",
            Slug = "deleted",
            IsActive = false
        };

        await _service.LogChangeAsync(PricingAuditAction.Delete, null, tier, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(5, log.EntityId);
        Assert.Equal(PricingAuditAction.Delete, log.Action);
        Assert.Null(log.NewValues);
        Assert.NotNull(log.OldValues);
    }

    [Fact]
    public async Task LogChangeAsync_RecordsIpAddress()
    {
        SetupHttpContext("192.168.1.100");

        var tier = new PricingTier { Id = 1, Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("192.168.1.100", log.IpAddress);
    }

    [Fact]
    public async Task LogChangeAsync_RecordsUserAgent()
    {
        SetupHttpContext(userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var tier = new PricingTier { Id = 1, Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Contains("Mozilla", log.UserAgent);
    }

    [Fact]
    public async Task LogChangeAsync_TruncatesLongUserAgent()
    {
        var longUserAgent = new string('x', 600);
        SetupHttpContext(userAgent: longUserAgent);

        var tier = new PricingTier { Id = 1, Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.NotNull(log.UserAgent);
        Assert.Equal(500, log.UserAgent.Length);
    }

    [Fact]
    public async Task LogChangeAsync_UsesXForwardedForHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.5, 10.0.0.2";
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);

        var tier = new PricingTier { Id = 1, Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("203.0.113.5", log.IpAddress);
    }

    [Fact]
    public async Task LogChangeAsync_GeneratesChangeDescription()
    {
        var tier = new PricingTier
        {
            Id = 1,
            Name = "Craft",
            Slug = "craft",
            IsActive = true
        };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Contains("Pricing tier 'Craft'", log.ChangeDescription);
        Assert.Contains("was created", log.ChangeDescription);
    }

    [Fact]
    public async Task LogChangeAsync_HandlesNullHttpContext()
    {
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        var tier = new PricingTier { Id = 1, Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Null(log.IpAddress);
        Assert.Null(log.UserAgent);
    }

    [Fact]
    public async Task LogChangeAsync_DoesNotLogWhenEntityIdCannotBeDetermined()
    {
        // Create a tier without an ID set
        var tier = new PricingTier { Name = "Test", Slug = "test" };

        await _service.LogChangeAsync(PricingAuditAction.Create, tier, null, 1);

        var log = await _context.PricingAuditLogs.FirstOrDefaultAsync();
        Assert.Null(log);
    }

    #endregion

    #region GetAuditLogsAsync Tests

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsAllLogs()
    {
        // Seed some logs
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync()).ToList();

        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByEntityType()
    {
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync(entityType: "PricingTier")).ToList();

        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Equal("PricingTier", l.EntityType));
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByEntityId()
    {
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync(entityId: 1)).ToList();

        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByDateRange()
    {
        await SeedAuditLogs();

        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow.AddHours(1);

        var logs = (await _service.GetAuditLogsAsync(startDate: startDate, endDate: endDate)).ToList();

        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task GetAuditLogsAsync_RespectsLimit()
    {
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync(limit: 2)).ToList();

        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task GetAuditLogsAsync_OrdersByTimestampDescending()
    {
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync()).ToList();

        Assert.True(logs[0].ChangeTimestamp >= logs[1].ChangeTimestamp);
        Assert.True(logs[1].ChangeTimestamp >= logs[2].ChangeTimestamp);
    }

    [Fact]
    public async Task GetAuditLogsAsync_CombinesFilters()
    {
        await SeedAuditLogs();

        var logs = (await _service.GetAuditLogsAsync(
            entityType: "PricingTier",
            entityId: 1)).ToList();

        Assert.Single(logs);
    }

    private async Task SeedAuditLogs()
    {
        // Seed required users first (for the foreign key relationship)
        var users = new List<User>
        {
            new() { Id = 1, Name = "Test User 1", Email = "user1@test.com", UserTypeId = 1, CompanyId = 1 },
            new() { Id = 2, Name = "Test User 2", Email = "user2@test.com", UserTypeId = 1, CompanyId = 1 }
        };
        _context.Users.AddRange(users);

        var logs = new List<PricingAuditLog>
        {
            new()
            {
                EntityType = "PricingTier",
                EntityId = 1,
                Action = PricingAuditAction.Create,
                ChangedByUserId = 1,
                ChangeTimestamp = DateTime.UtcNow.AddMinutes(-30),
                ChangeDescription = "Created tier"
            },
            new()
            {
                EntityType = "PricingTier",
                EntityId = 2,
                Action = PricingAuditAction.Update,
                ChangedByUserId = 1,
                ChangeTimestamp = DateTime.UtcNow.AddMinutes(-20),
                ChangeDescription = "Updated tier"
            },
            new()
            {
                EntityType = "PricingFeature",
                EntityId = 1,
                Action = PricingAuditAction.Create,
                ChangedByUserId = 2,
                ChangeTimestamp = DateTime.UtcNow.AddMinutes(-10),
                ChangeDescription = "Created feature"
            }
        };

        _context.PricingAuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();
    }

    #endregion
}
