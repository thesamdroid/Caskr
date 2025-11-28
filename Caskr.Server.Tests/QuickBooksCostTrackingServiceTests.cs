using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Intuit.Ipp.Core;
using QuickBooksJournalEntry = Intuit.Ipp.Data.JournalEntry;
using Intuit.Ipp.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksCostTrackingServiceTests
{
    private static DbContextOptions<CaskrDbContext> BuildOptions()
        => new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task RecordBatchCOGSAsync_ReturnsExistingJournalEntry_WhenAlreadySynced()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var (batch, company) = await SeedBatchAsync(context, includeCostLineItems: true);

        context.AccountingSyncLogs.Add(new AccountingSyncLog
        {
            CompanyId = company.Id,
            EntityType = "Batch",
            EntityId = batch.Id.ToString(CultureInfo.InvariantCulture),
            ExternalEntityId = "QBO-JE-1",
            SyncStatus = SyncStatus.Success,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var contextFactory = BuildContextFactoryMock(batch.CompanyId);
        var journalClient = new Mock<IQuickBooksJournalEntryClient>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<QuickBooksCostTrackingService>>();
        var syncLogService = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);
        var accountMappingService = new QuickBooksAccountMappingService(context, NullLogger<QuickBooksAccountMappingService>.Instance);
        var service = new QuickBooksCostTrackingService(
            context,
            contextFactory.Object,
            journalClient.Object,
            syncLogService,
            accountMappingService,
            logger);

        var result = await service.RecordBatchCOGSAsync(batch.Id);

        Assert.True(result.Success);
        Assert.Equal("QBO-JE-1", result.QboJournalEntryId);
        journalClient.Verify(c => c.CreateJournalEntryAsync(
            It.IsAny<ServiceContext>(),
            It.IsAny<QuickBooksJournalEntry>(),
            It.IsAny<System.Threading.CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordBatchCOGSAsync_CreatesJournalEntry_WhenCostsAvailable()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var (batch, company) = await SeedBatchAsync(context, includeCostLineItems: true);

        var contextFactory = BuildContextFactoryMock(company.Id);

        var journalClient = new Mock<IQuickBooksJournalEntryClient>();
        journalClient.Setup(c => c.CreateJournalEntryAsync(
                It.IsAny<ServiceContext>(),
                It.IsAny<QuickBooksJournalEntry>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new QuickBooksJournalEntry { Id = "JE-900" });

        var logger = Mock.Of<ILogger<QuickBooksCostTrackingService>>();
        var syncLogService = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);
        var accountMappingService = new QuickBooksAccountMappingService(context, NullLogger<QuickBooksAccountMappingService>.Instance);
        var service = new QuickBooksCostTrackingService(
            context,
            contextFactory.Object,
            journalClient.Object,
            syncLogService,
            accountMappingService,
            logger);

        var result = await service.RecordBatchCOGSAsync(batch.Id);

        Assert.True(result.Success);
        Assert.Equal("JE-900", result.QboJournalEntryId);

        var log = await context.AccountingSyncLogs.SingleAsync(l => l.EntityId == batch.Id.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(SyncStatus.Success, log.SyncStatus);
        Assert.Equal("JE-900", log.ExternalEntityId);
    }

    [Fact]
    public async Task RecordBatchCOGSAsync_ReturnsFailure_WhenNoCostComponents()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var (batch, company) = await SeedBatchAsync(context, includeCostLineItems: false);

        var contextFactory = BuildContextFactoryMock(company.Id);

        var journalClient = new Mock<IQuickBooksJournalEntryClient>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<QuickBooksCostTrackingService>>();
        var syncLogService = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);
        var accountMappingService = new QuickBooksAccountMappingService(context, NullLogger<QuickBooksAccountMappingService>.Instance);
        var service = new QuickBooksCostTrackingService(
            context,
            contextFactory.Object,
            journalClient.Object,
            syncLogService,
            accountMappingService,
            logger);

        var result = await service.RecordBatchCOGSAsync(batch.Id);

        Assert.False(result.Success);
        Assert.Null(result.QboJournalEntryId);
        Assert.NotNull(result.ErrorMessage);
        journalClient.Verify(c => c.CreateJournalEntryAsync(
            It.IsAny<ServiceContext>(),
            It.IsAny<QuickBooksJournalEntry>(),
            It.IsAny<System.Threading.CancellationToken>()), Times.Never);

        var log = await context.AccountingSyncLogs.SingleAsync(l => l.EntityId == batch.Id.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(SyncStatus.Failed, log.SyncStatus);
    }

    [Fact]
    public async Task RecordBatchCOGSAsync_SetsJournalEntryDescription()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        const string orderName = "Heritage Reserve";
        var (batch, company) = await SeedBatchAsync(context, includeCostLineItems: true, orderName: orderName);

        var contextFactory = BuildContextFactoryMock(company.Id);

        QuickBooksJournalEntry? capturedEntry = null;
        var journalClient = new Mock<IQuickBooksJournalEntryClient>();
        journalClient.Setup(c => c.CreateJournalEntryAsync(
                It.IsAny<ServiceContext>(),
                It.IsAny<QuickBooksJournalEntry>(),
                It.IsAny<CancellationToken>()))
            .Callback<ServiceContext, QuickBooksJournalEntry, CancellationToken>((_, entry, _) => capturedEntry = entry)
            .ReturnsAsync((ServiceContext _, QuickBooksJournalEntry entry, CancellationToken _) =>
            {
                entry.Id = "JE-777";
                return entry;
            });

        var logger = Mock.Of<ILogger<QuickBooksCostTrackingService>>();
        var syncLogService = new QuickBooksSyncLogService(context, NullLogger<QuickBooksSyncLogService>.Instance);
        var accountMappingService = new QuickBooksAccountMappingService(context, NullLogger<QuickBooksAccountMappingService>.Instance);
        var service = new QuickBooksCostTrackingService(
            context,
            contextFactory.Object,
            journalClient.Object,
            syncLogService,
            accountMappingService,
            logger);

        var result = await service.RecordBatchCOGSAsync(batch.Id);

        Assert.True(result.Success);
        Assert.NotNull(capturedEntry);
        var expectedDescription = $"Batch {batch.Id} completion - {orderName}";
        Assert.Equal(expectedDescription, capturedEntry!.PrivateNote);
        Assert.NotNull(capturedEntry.Line);
        Assert.All(capturedEntry.Line!, line => Assert.Equal(expectedDescription, line.Description));
    }

    private static async Task<(Batch Batch, Company Company)> SeedBatchAsync(
        CaskrDbContext context,
        bool includeCostLineItems,
        string orderName = "Reserve Batch")
    {
        var company = new Company
        {
            CompanyName = "Heritage Spirits",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company);

        var userType = new UserType { Id = 90, Name = "Admin" };
        var status = new Status { Id = 80, Name = "In Progress" };
        var spiritType = new SpiritType { Id = 70, Name = "Bourbon" };
        context.UserTypes.Add(userType);
        context.Statuses.Add(status);
        context.SpiritTypes.Add(spiritType);
        await context.SaveChangesAsync();

        var user = new User
        {
            Id = 400,
            Name = "Owner",
            Email = "owner@example.com",
            CompanyId = company.Id,
            UserTypeId = userType.Id,
            IsPrimaryContact = true
        };
        context.Users.Add(user);

        var mashBill = new MashBill
        {
            Id = 60,
            CompanyId = company.Id,
            Name = "Signature"
        };
        context.MashBills.Add(mashBill);
        await context.SaveChangesAsync();

        var batch = new Batch
        {
            Id = 500,
            CompanyId = company.Id,
            MashBillId = mashBill.Id
        };
        context.Batches.Add(batch);

        var invoice = new Invoice
        {
            CompanyId = company.Id,
            InvoiceNumber = "INV-1",
            CustomerName = "Internal",
            InvoiceDate = DateTime.UtcNow.Date,
            SubtotalAmount = includeCostLineItems ? 200m : 100m,
            TotalAmount = includeCostLineItems ? 200m : 100m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (includeCostLineItems)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Grain",
                Quantity = 1,
                UnitPrice = 150m,
                AccountType = CaskrAccountType.RawMaterials,
                IsTaxable = false
            });
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Barrels",
                Quantity = 1,
                UnitPrice = 50m,
                AccountType = CaskrAccountType.Barrels,
                IsTaxable = false
            });
        }
        else
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Finished Goods",
                Quantity = 1,
                UnitPrice = 100m,
                AccountType = CaskrAccountType.FinishedGoods,
                IsTaxable = false
            });
        }

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var order = new Order
        {
            Id = 320,
            Name = orderName,
            OwnerId = user.Id,
            CompanyId = company.Id,
            StatusId = status.Id,
            SpiritTypeId = spiritType.Id,
            BatchId = batch.Id,
            Quantity = 1,
            InvoiceId = invoice.Id,
            Invoice = invoice
        };
        context.Orders.Add(order);

        context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = company.Id,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.ChartOfAccountsMappings.AddRange(
            new ChartOfAccountsMapping
            {
                CompanyId = company.Id,
                CaskrAccountType = CaskrAccountType.Cogs,
                QboAccountId = "acct-cogs",
                QboAccountName = "COGS",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ChartOfAccountsMapping
            {
                CompanyId = company.Id,
                CaskrAccountType = CaskrAccountType.WorkInProgress,
                QboAccountId = "acct-wip",
                QboAccountName = "WIP",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        return (batch, company);
    }

    private static Mock<IQuickBooksIntegrationContextFactory> BuildContextFactoryMock(int companyId)
    {
        var factory = new Mock<IQuickBooksIntegrationContextFactory>();
        var validator = new OAuth2RequestValidator("token");
        var context = new ServiceContext("realm", IntuitServicesType.QBO, validator);
        factory.Setup(f => f.CreateAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickBooksIntegrationContext(companyId, "realm", context));
        return factory;
    }
}
