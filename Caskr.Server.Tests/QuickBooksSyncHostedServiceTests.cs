using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Services;
using Caskr.Server.Services.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksSyncHostedServiceTests
{
    [Fact]
    public async Task ProcessCompaniesAsync_SyncsPendingInvoicesAndBatches_WhenPreferenceDue()
    {
        var invoiceSync = new Mock<IQuickBooksInvoiceSyncService>();
        invoiceSync
            .Setup(service => service.SyncInvoiceToQBOAsync(It.IsAny<int>()))
            .ReturnsAsync(new InvoiceSyncResult(true, "qb-invoice", null));

        var costTracking = new Mock<IQuickBooksCostTrackingService>();
        costTracking
            .Setup(service => service.RecordBatchCOGSAsync(It.IsAny<int>()))
            .ReturnsAsync(new JournalEntrySyncResult(true, "qb-journal", null));

        using var provider = BuildServiceProvider(invoiceSync, costTracking);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        await SeedCompanyDataAsync(provider, "Hourly");

        var logger = Mock.Of<ILogger<QuickBooksSyncHostedService>>();
        var service = new QuickBooksSyncHostedService(scopeFactory, logger);

        var delay = await service.ProcessCompaniesAsync(CancellationToken.None);

        invoiceSync.Verify(serviceMock => serviceMock.SyncInvoiceToQBOAsync(101), Times.Once);
        costTracking.Verify(serviceMock => serviceMock.RecordBatchCOGSAsync(202), Times.Once);

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
            var preference = await dbContext.AccountingSyncPreferences.SingleAsync();
            Assert.NotNull(preference.LastSyncAt);
        }

        Assert.True(delay <= TimeSpan.FromHours(1));
        Assert.True(delay >= TimeSpan.FromMinutes(59));
    }

    [Fact]
    public async Task ProcessCompaniesAsync_SkipsManualPreferences()
    {
        var invoiceSync = new Mock<IQuickBooksInvoiceSyncService>(MockBehavior.Strict);
        var costTracking = new Mock<IQuickBooksCostTrackingService>(MockBehavior.Strict);

        using var provider = BuildServiceProvider(invoiceSync, costTracking);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        await SeedCompanyDataAsync(provider, "Manual");

        var logger = Mock.Of<ILogger<QuickBooksSyncHostedService>>();
        var service = new QuickBooksSyncHostedService(scopeFactory, logger);

        var delay = await service.ProcessCompaniesAsync(CancellationToken.None);

        invoiceSync.Verify(serviceMock => serviceMock.SyncInvoiceToQBOAsync(It.IsAny<int>()), Times.Never);
        costTracking.Verify(serviceMock => serviceMock.RecordBatchCOGSAsync(It.IsAny<int>()), Times.Never);
        Assert.Equal(TimeSpan.FromMinutes(5), delay);
    }

    private static ServiceProvider BuildServiceProvider(
        Mock<IQuickBooksInvoiceSyncService> invoiceSync,
        Mock<IQuickBooksCostTrackingService> costTracking)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<CaskrDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped(_ => invoiceSync.Object);
        services.AddScoped(_ => costTracking.Object);
        return services.BuildServiceProvider();
    }

    private static async Task SeedCompanyDataAsync(ServiceProvider provider, string frequency)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();

        dbContext.AccountingIntegrations.RemoveRange(dbContext.AccountingIntegrations);
        dbContext.AccountingSyncPreferences.RemoveRange(dbContext.AccountingSyncPreferences);
        dbContext.AccountingSyncLogs.RemoveRange(dbContext.AccountingSyncLogs);
        dbContext.Companies.RemoveRange(dbContext.Companies);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        dbContext.Companies.Add(new Company
        {
            Id = 1,
            CompanyName = "Test Company",
            CreatedAt = now,
            RenewalDate = now
        });

        dbContext.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = 1,
            Provider = AccountingProvider.QuickBooks,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        dbContext.AccountingSyncPreferences.Add(new AccountingSyncPreference
        {
            CompanyId = 1,
            Provider = AccountingProvider.QuickBooks,
            AutoSyncInvoices = true,
            AutoSyncCogs = true,
            SyncFrequency = frequency,
            LastSyncAt = frequency == "Manual" ? now : now.AddHours(-2),
            CreatedAt = now,
            UpdatedAt = now
        });

        dbContext.AccountingSyncLogs.AddRange(
            new AccountingSyncLog
            {
                CompanyId = 1,
                EntityType = "Invoice",
                EntityId = "101",
                SyncStatus = SyncStatus.Pending,
                RetryCount = 0,
                SyncedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new AccountingSyncLog
            {
                CompanyId = 1,
                EntityType = "Batch",
                EntityId = "202",
                SyncStatus = SyncStatus.Pending,
                RetryCount = 0,
                SyncedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

        await dbContext.SaveChangesAsync();
    }
}
