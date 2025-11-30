using System;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksSyncLogServiceTests
{
    private static DbContextOptions<CaskrDbContext> BuildOptions() => new DbContextOptionsBuilder<CaskrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task GetOrCreateSyncLogAsync_ReturnsExistingLogWithoutCreatingDuplicate()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var existingLog = new AccountingSyncLog
        {
            CompanyId = 7,
            EntityType = "Invoice",
            EntityId = "INV-001",
            ExternalEntityId = "QBO-9",
            SyncStatus = SyncStatus.Success,
            RetryCount = 0,
            SyncedAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.AccountingSyncLogs.Add(existingLog);
        await context.SaveChangesAsync();

        var service = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);

        var result = await service.GetOrCreateSyncLogAsync(7, "Invoice", "INV-001");

        Assert.Equal(existingLog.Id, result.Id);
        Assert.Equal(1, await context.AccountingSyncLogs.CountAsync());
    }

    [Fact]
    public async Task GetOrCreateSyncLogAsync_ThrowsWhenCompanyIdNonPositive()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var service = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetOrCreateSyncLogAsync(0, "Invoice", "INV-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrCreateSyncLogAsync_ThrowsWhenEntityTypeMissing(string entityType)
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var service = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetOrCreateSyncLogAsync(3, entityType, "INV-2"));
    }

    [Fact]
    public async Task GetSuccessfulSyncExternalIdAsync_ReturnsMostRecentSuccessId()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var service = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);

        context.AccountingSyncLogs.AddRange(
            new AccountingSyncLog
            {
                CompanyId = 9,
                EntityType = "Invoice",
                EntityId = "INV-500",
                ExternalEntityId = "QBO-OLD",
                SyncStatus = SyncStatus.Success,
                RetryCount = 0,
                SyncedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new AccountingSyncLog
            {
                CompanyId = 9,
                EntityType = "Invoice",
                EntityId = "INV-500",
                ExternalEntityId = null,
                SyncStatus = SyncStatus.Failed,
                RetryCount = 1,
                ErrorMessage = "Network error",
                SyncedAt = DateTime.UtcNow.AddHours(-1.5),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-1.5)
            },
            new AccountingSyncLog
            {
                CompanyId = 9,
                EntityType = "Invoice",
                EntityId = "INV-500",
                ExternalEntityId = "QBO-NEW",
                SyncStatus = SyncStatus.Success,
                RetryCount = 1,
                SyncedAt = DateTime.UtcNow.AddMinutes(-30),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
            });
        await context.SaveChangesAsync();

        var externalId = await service.GetSuccessfulSyncExternalIdAsync(9, "Invoice", "INV-500");

        Assert.Equal("QBO-NEW", externalId);
    }

    [Theory]
    [InlineData(0, "Invoice", "INV-9")]
    [InlineData(1, "Invoice", " ")]
    public async Task GetSuccessfulSyncExternalIdAsync_ValidatesInputs(int companyId, string entityType, string entityId)
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var service = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);

        if (companyId <= 0)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => service.GetSuccessfulSyncExternalIdAsync(companyId, entityType, entityId));
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetSuccessfulSyncExternalIdAsync(companyId, entityType, entityId));
        }
    }
}
