using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using InvoiceModel = Caskr.server.Models.Invoice;
using QuickBooksInvoice = Intuit.Ipp.Data.Invoice;
using CompanyModel = Caskr.server.Models.Company;
using Task = System.Threading.Tasks.Task;

namespace Caskr.Server.Tests.Integration;

public class QuickBooksIntegrationTests
{
    private static DbContextOptions<CaskrDbContext> BuildOptions()
        => new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task OAuthCallback_PersistsEncryptedTokens()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        var configuration = BuildConfiguration();
        IDataProtectionProvider protectorProvider = new EphemeralDataProtectionProvider();

        var oauthClientMock = new Mock<IQuickBooksOAuthClient>();
        oauthClientMock.Setup(c => c.ExchangeCodeForTokenAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTokenResponse("access-token", "refresh-token"));

        var clientFactoryMock = new Mock<IQuickBooksOAuthClientFactory>();
        clientFactoryMock.Setup(f => f.Create("client-id", "client-secret", "https://app.caskr.local/oauth", "sandbox"))
            .Returns(oauthClientMock.Object);

        var service = new QuickBooksAuthService(
            configuration,
            context,
            protectorProvider,
            NullLogger<QuickBooksAuthService>.Instance,
            clientFactoryMock.Object);

        var response = await service.HandleCallbackAsync("auth-code", "realm-123", company.Id);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal("realm-123", response.RealmId);

        var integration = await context.AccountingIntegrations.SingleAsync(ai => ai.CompanyId == company.Id);
        Assert.NotNull(integration.AccessTokenEncrypted);
        Assert.NotNull(integration.RefreshTokenEncrypted);
        Assert.DoesNotContain("access-token", integration.AccessTokenEncrypted!, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-token", integration.RefreshTokenEncrypted!, StringComparison.Ordinal);

        var protector = protectorProvider.CreateProtector("Caskr.Server.Services.QuickBooksAuthService.Tokens");
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var accessPayload = JsonSerializer.Deserialize<TokenPayload>(
            protector.Unprotect(integration.AccessTokenEncrypted!),
            jsonOptions);
        var refreshPayload = JsonSerializer.Deserialize<TokenPayload>(
            protector.Unprotect(integration.RefreshTokenEncrypted!),
            jsonOptions);

        Assert.Equal("access-token", accessPayload!.Token);
        Assert.Equal("refresh-token", refreshPayload!.Token);
    }

    [Fact]
    public async Task ChartOfAccounts_FetchesOnceAndCaches()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        await SeedQuickBooksIntegrationAsync(context, company.Id);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var authServiceMock = new Mock<IQuickBooksAuthService>();
        authServiceMock.Setup(a => a.RefreshTokenAsync(company.Id))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 3600,
                RealmId = "realm-1"
            });

        var accountClientMock = new Mock<IQuickBooksAccountQueryClient>();
        accountClientMock.Setup(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()))
            .Returns(new List<Account>
            {
                new() { Id = "1", Name = "Sales", AccountType = AccountTypeEnum.Income, AccountTypeSpecified = true, Active = true, ActiveSpecified = true },
                new() { Id = "2", Name = "COGS", AccountType = AccountTypeEnum.CostofGoodsSold, AccountTypeSpecified = true, Active = true, ActiveSpecified = true }
            });

        var contextFactory = new QuickBooksIntegrationContextFactory(
            context,
            authServiceMock.Object,
            NullLogger<QuickBooksIntegrationContextFactory>.Instance);

        var service = new QuickBooksDataService(
            cache,
            contextFactory,
            accountClientMock.Object,
            NullLogger<QuickBooksDataService>.Instance);

        var firstResult = await service.GetChartOfAccountsAsync(company.Id);
        var secondResult = await service.GetChartOfAccountsAsync(company.Id);

        Assert.Equal(2, firstResult.Count);
        Assert.Same(firstResult, secondResult);
        accountClientMock.Verify(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()), Times.Once);
    }

    [Fact]
    public async Task InvoiceSync_SuccessfullyCreatesQuickBooksInvoice()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        var integration = await SeedQuickBooksIntegrationAsync(context, company.Id);
        var invoice = await SeedInvoiceAsync(context, company.Id, hasSecondLine: false);

        var authServiceMock = BuildAuthServiceMock(company.Id);
        var clientMock = BuildInvoiceClientMock(invoice, out _);
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QuickBooksInvoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickBooksInvoice { Id = "INV-300" });

        var contextFactory = BuildIntegrationContextFactory(context, authServiceMock);
        var service = new QuickBooksInvoiceSyncService(
            context,
            contextFactory,
            clientMock.Object,
            NullLogger<QuickBooksInvoiceSyncService>.Instance);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.True(result.Success);
        Assert.Equal("INV-300", result.QboInvoiceId);

        var log = await context.AccountingSyncLogs.SingleAsync();
        Assert.Equal(SyncStatus.Success, log.SyncStatus);
        Assert.Equal("INV-300", log.ExternalEntityId);
        Assert.Equal(integration.CompanyId, log.CompanyId);
    }

    [Fact]
    public async Task InvoiceSync_RecordsFailureWhenQuickBooksErrors()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        await SeedQuickBooksIntegrationAsync(context, company.Id);
        var invoice = await SeedInvoiceAsync(context, company.Id, hasSecondLine: false);

        var authServiceMock = BuildAuthServiceMock(company.Id);
        var clientMock = BuildInvoiceClientMock(invoice, out _);
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QuickBooksInvoice>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid customer"));

        var contextFactory = BuildIntegrationContextFactory(context, authServiceMock);
        var service = new QuickBooksInvoiceSyncService(
            context,
            contextFactory,
            clientMock.Object,
            NullLogger<QuickBooksInvoiceSyncService>.Instance);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.False(result.Success);
        Assert.Contains("invalid customer", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var log = await context.AccountingSyncLogs.SingleAsync();
        Assert.Equal(SyncStatus.Failed, log.SyncStatus);
        Assert.Equal("invalid customer", log.ErrorMessage);
    }

    [Fact]
    public async Task CostTracking_CreatesCogsJournalEntry()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        await SeedQuickBooksIntegrationAsync(context, company.Id);
        var batch = await SeedBatchWithOrderAndInvoiceAsync(context, company.Id);
        await SeedCostAccountMappingsAsync(context, company.Id);

        var authServiceMock = BuildAuthServiceMock(company.Id);
        JournalEntry? capturedEntry = null;
        var journalClientMock = new Mock<IQuickBooksJournalEntryClient>();
        journalClientMock.Setup(c => c.CreateJournalEntryAsync(It.IsAny<ServiceContext>(), It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns<ServiceContext, JournalEntry, CancellationToken>((_, entry, _) =>
            {
                capturedEntry = entry;
                entry.Id = "JE-777";
                return Task.FromResult(entry);
            });

        var contextFactory = BuildIntegrationContextFactory(context, authServiceMock);
        var service = new QuickBooksCostTrackingService(
            context,
            contextFactory,
            journalClientMock.Object,
            NullLogger<QuickBooksCostTrackingService>.Instance);

        var result = await service.RecordBatchCOGSAsync(batch.Id);

        Assert.True(result.Success);
        Assert.Equal("JE-777", result.QboJournalEntryId);
        Assert.NotNull(capturedEntry);
        Assert.Equal(2, capturedEntry!.Line?.Length);

        var debit = capturedEntry.Line![0];
        var debitDetail = Assert.IsType<JournalEntryLineDetail>(debit.AnyIntuitObject);
        Assert.Equal("acct-cogs", debitDetail.AccountRef?.Value);
        Assert.Equal(PostingTypeEnum.Debit, debitDetail.PostingType);

        var credit = capturedEntry.Line![1];
        var creditDetail = Assert.IsType<JournalEntryLineDetail>(credit.AnyIntuitObject);
        Assert.Equal("acct-wip", creditDetail.AccountRef?.Value);
        Assert.Equal(PostingTypeEnum.Credit, creditDetail.PostingType);

        var log = await context.AccountingSyncLogs.SingleAsync(l => l.EntityType == "Batch");
        Assert.Equal(SyncStatus.Success, log.SyncStatus);
        Assert.Equal("JE-777", log.ExternalEntityId);
    }

    [Fact]
    public async Task InvoiceSync_UsesAccountMappingsForLineItems()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        await SeedQuickBooksIntegrationAsync(context, company.Id);
        var invoice = await SeedInvoiceAsync(context, company.Id, hasSecondLine: true, includeDefaultMapping: false);

        context.ChartOfAccountsMappings.AddRange(
            CreateMapping(company.Id, CaskrAccountType.FinishedGoods, "acct-fg", "Finished Goods"),
            CreateMapping(company.Id, CaskrAccountType.RawMaterials, "acct-rm", "Raw Materials"));
        await context.SaveChangesAsync();

        var authServiceMock = BuildAuthServiceMock(company.Id);
        QuickBooksInvoice? capturedInvoice = null;
        var clientMock = BuildInvoiceClientMock(invoice, out var existingCustomer);
        clientMock.Setup(c => c.FindCustomerByDisplayNameAsync(It.IsAny<ServiceContext>(), invoice.CustomerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QuickBooksInvoice>(), It.IsAny<CancellationToken>()))
            .Returns<ServiceContext, QuickBooksInvoice, CancellationToken>((_, qbInvoice, _) =>
            {
                capturedInvoice = qbInvoice;
                return Task.FromResult(new QuickBooksInvoice { Id = "INV-555" });
            });

        var contextFactory = BuildIntegrationContextFactory(context, authServiceMock);
        var service = new QuickBooksInvoiceSyncService(
            context,
            contextFactory,
            clientMock.Object,
            NullLogger<QuickBooksInvoiceSyncService>.Instance);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);
        Assert.True(result.Success);
        Assert.NotNull(capturedInvoice);
        Assert.Equal(2, capturedInvoice!.Line?.Length);

        var firstDetail = Assert.IsType<SalesItemLineDetail>(capturedInvoice.Line![0].AnyIntuitObject);
        Assert.Equal("acct-fg", firstDetail.ItemRef?.Value);
        var secondDetail = Assert.IsType<SalesItemLineDetail>(capturedInvoice.Line![1].AnyIntuitObject);
        Assert.Equal("acct-rm", secondDetail.ItemRef?.Value);
    }

    [Fact]
    public async Task InvoiceSync_IsIdempotentWhenPreviouslySuccessful()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var company = await SeedCompanyAsync(context);
        await SeedQuickBooksIntegrationAsync(context, company.Id);
        var invoice = await SeedInvoiceAsync(context, company.Id, hasSecondLine: false);

        var authServiceMock = BuildAuthServiceMock(company.Id);
        var clientMock = BuildInvoiceClientMock(invoice, out _);
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QuickBooksInvoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickBooksInvoice { Id = "INV-867" });

        var contextFactory = BuildIntegrationContextFactory(context, authServiceMock);
        var service = new QuickBooksInvoiceSyncService(
            context,
            contextFactory,
            clientMock.Object,
            NullLogger<QuickBooksInvoiceSyncService>.Instance);

        var firstResult = await service.SyncInvoiceToQBOAsync(invoice.Id);
        var secondResult = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);
        Assert.Equal("INV-867", secondResult.QboInvoiceId);
        clientMock.Verify(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QuickBooksInvoice>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IConfiguration BuildConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["QuickBooks:ClientId"] = "client-id",
            ["QuickBooks:ClientSecret"] = "client-secret",
            ["QuickBooks:RedirectUri"] = "https://app.caskr.local/oauth",
            ["QuickBooks:Environment"] = "sandbox"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static Mock<IQuickBooksAuthService> BuildAuthServiceMock(int companyId)
    {
        var mock = new Mock<IQuickBooksAuthService>();
        mock.Setup(a => a.RefreshTokenAsync(companyId))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                RealmId = "realm-1",
                ExpiresIn = 3600
            });
        return mock;
    }

    private static QuickBooksIntegrationContextFactory BuildIntegrationContextFactory(
        CaskrDbContext context,
        Mock<IQuickBooksAuthService> authServiceMock)
    {
        return new QuickBooksIntegrationContextFactory(
            context,
            authServiceMock.Object,
            NullLogger<QuickBooksIntegrationContextFactory>.Instance);
    }

    private static Mock<IQuickBooksInvoiceClient> BuildInvoiceClientMock(InvoiceModel invoice, out Intuit.Ipp.Data.Customer existingCustomer)
    {
        var mock = new Mock<IQuickBooksInvoiceClient>();
        existingCustomer = new Intuit.Ipp.Data.Customer { Id = "cust-1", DisplayName = invoice.CustomerName };
        mock.Setup(c => c.FindCustomerByEmailAsync(It.IsAny<ServiceContext>(), invoice.CustomerEmail!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Intuit.Ipp.Data.Customer?)null);
        mock.Setup(c => c.FindCustomerByDisplayNameAsync(It.IsAny<ServiceContext>(), invoice.CustomerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Intuit.Ipp.Data.Customer?)null);
        mock.Setup(c => c.CreateCustomerAsync(It.IsAny<ServiceContext>(), It.IsAny<Intuit.Ipp.Data.Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);
        return mock;
    }

    private static async Task<CompanyModel> SeedCompanyAsync(CaskrDbContext context)
    {
        var company = new CompanyModel
        {
            CompanyName = "Integration Co",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return company;
    }

    private static async Task<AccountingIntegration> SeedQuickBooksIntegrationAsync(CaskrDbContext context, int companyId)
    {
        var integration = new AccountingIntegration
        {
            CompanyId = companyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm-1",
            IsActive = true,
            AccessTokenEncrypted = "stub",
            RefreshTokenEncrypted = "stub",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.AccountingIntegrations.Add(integration);
        await context.SaveChangesAsync();
        return integration;
    }

    private static async Task<InvoiceModel> SeedInvoiceAsync(
        CaskrDbContext context,
        int companyId,
        bool hasSecondLine,
        bool includeDefaultMapping = true)
    {
        var invoice = new InvoiceModel
        {
            CompanyId = companyId,
            InvoiceNumber = "INV-1",
            CustomerName = "Sample Customer",
            CustomerEmail = "customer@example.com",
            CustomerPhone = "555-5555",
            CustomerAddressLine1 = "123 Test St",
            CustomerCity = "Louisville",
            CustomerState = "KY",
            CustomerPostalCode = "40202",
            CurrencyCode = "USD",
            InvoiceDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            SubtotalAmount = 150m,
            TotalAmount = 159m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LineItems =
            {
                new InvoiceLineItem
                {
                    Description = "Bottling",
                    Quantity = 1,
                    UnitPrice = 100m,
                    AccountType = CaskrAccountType.FinishedGoods,
                    IsTaxable = true
                }
            },
            Taxes =
            {
                new InvoiceTax
                {
                    TaxName = "Sales Tax",
                    TaxCode = "TAX",
                    Rate = 6m,
                    Amount = 9m
                }
            }
        };

        if (hasSecondLine)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Raw Material",
                Quantity = 1,
                UnitPrice = 50m,
                AccountType = CaskrAccountType.RawMaterials,
                IsTaxable = false
            });
        }

        context.Invoices.Add(invoice);
        if (includeDefaultMapping)
        {
            context.ChartOfAccountsMappings.Add(
                CreateMapping(companyId, CaskrAccountType.FinishedGoods, "acct-fg", "Finished Goods"));
        }
        await context.SaveChangesAsync();
        return invoice;
    }

    private static ChartOfAccountsMapping CreateMapping(int companyId, CaskrAccountType type, string accountId, string? name)
    {
        return new ChartOfAccountsMapping
        {
            CompanyId = companyId,
            CaskrAccountType = type,
            QboAccountId = accountId,
            QboAccountName = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static async Task<Batch> SeedBatchWithOrderAndInvoiceAsync(CaskrDbContext context, int companyId)
    {
        var mashBill = new MashBill
        {
            Id = 1,
            CompanyId = companyId,
            Name = "MB-1"
        };
        context.MashBills.Add(mashBill);
        await context.SaveChangesAsync();

        var batch = new Batch
        {
            Id = 1,
            CompanyId = companyId,
            MashBillId = mashBill.Id,
            MashBill = mashBill
        };
        context.Batches.Add(batch);
        await context.SaveChangesAsync();

        var invoice = new InvoiceModel
        {
            CompanyId = companyId,
            InvoiceNumber = "INV-COST",
            CustomerName = "Cost Customer",
            InvoiceDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LineItems =
            {
                new InvoiceLineItem
                {
                    Description = "Grain",
                    Quantity = 10,
                    UnitPrice = 5m,
                    AccountType = CaskrAccountType.RawMaterials,
                    IsTaxable = false
                }
            }
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var order = new Order
        {
            Name = "Order-1",
            OwnerId = 1,
            CompanyId = companyId,
            StatusId = 1,
            SpiritTypeId = 1,
            BatchId = batch.Id,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InvoiceId = invoice.Id
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return batch;
    }

    private static async Task SeedCostAccountMappingsAsync(CaskrDbContext context, int companyId)
    {
        context.ChartOfAccountsMappings.AddRange(
            CreateMapping(companyId, CaskrAccountType.Cogs, "acct-cogs", "COGS"),
            CreateMapping(companyId, CaskrAccountType.WorkInProgress, "acct-wip", "WIP"));
        await context.SaveChangesAsync();
    }

    private static TokenResponse BuildTokenResponse(string accessToken, string refreshToken)
    {
        var json = JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            expires_in = 3600,
            x_refresh_token_expires_in = 864000,
            token_type = "bearer"
        });

        return new TokenResponse(json);
    }

    private sealed record TokenPayload(string Token, long ExpiresInSeconds);
}
