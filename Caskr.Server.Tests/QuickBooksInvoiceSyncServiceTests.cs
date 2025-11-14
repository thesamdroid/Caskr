using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Intuit.Ipp.Core;
using QboCustomer = Intuit.Ipp.Data.Customer;
using QboInvoice = Intuit.Ipp.Data.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksInvoiceSyncServiceTests
{
    private static DbContextOptions<CaskrDbContext> BuildOptions()
        => new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task SyncInvoiceToQboAsync_ReturnsExistingQboId_WhenAlreadySynced()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var invoice = await SeedInvoiceAsync(context);
        context.AccountingSyncLogs.Add(new AccountingSyncLog
        {
            CompanyId = invoice.CompanyId,
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            ExternalEntityId = "QBO-42",
            SyncStatus = SyncStatus.Success,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var authService = Mock.Of<IQuickBooksAuthService>();
        var qbClientMock = new Mock<IQuickBooksInvoiceClient>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncService>>();
        var service = new QuickBooksInvoiceSyncService(context, authService, qbClientMock.Object, logger);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.True(result.Success);
        Assert.Equal("QBO-42", result.QboInvoiceId);
        qbClientMock.Verify(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QboInvoice>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncInvoiceToQboAsync_CreatesCustomerAndInvoice_WhenNotPreviouslySynced()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var invoice = await SeedInvoiceAsync(context);

        var authServiceMock = new Mock<IQuickBooksAuthService>();
        authServiceMock.Setup(a => a.RefreshTokenAsync(invoice.CompanyId))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                RealmId = "123"
            });

        var clientMock = new Mock<IQuickBooksInvoiceClient>();
        clientMock.Setup(c => c.FindCustomerByEmailAsync(It.IsAny<ServiceContext>(), invoice.CustomerEmail!, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((QboCustomer?)null);
        clientMock.Setup(c => c.FindCustomerByDisplayNameAsync(It.IsAny<ServiceContext>(), invoice.CustomerName, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((QboCustomer?)null);
        clientMock.Setup(c => c.CreateCustomerAsync(It.IsAny<ServiceContext>(), It.IsAny<QboCustomer>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new QboCustomer { Id = "cust-1", DisplayName = invoice.CustomerName });
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QboInvoice>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new QboInvoice { Id = "INV-100" });

        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncService>>();
        var service = new QuickBooksInvoiceSyncService(context, authServiceMock.Object, clientMock.Object, logger);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.True(result.Success);
        Assert.Equal("INV-100", result.QboInvoiceId);

        var log = await context.AccountingSyncLogs.SingleAsync(l => l.EntityId == invoice.Id.ToString());
        Assert.Equal(SyncStatus.Success, log.SyncStatus);
        Assert.Equal("INV-100", log.ExternalEntityId);
        Assert.Equal(0, log.RetryCount);
    }

    [Fact]
    public async Task SyncInvoiceToQboAsync_RetriesTransientFailures()
    {
        var options = BuildOptions();
        await using var context = new CaskrDbContext(options);
        var invoice = await SeedInvoiceAsync(context);

        var authServiceMock = new Mock<IQuickBooksAuthService>();
        authServiceMock.Setup(a => a.RefreshTokenAsync(invoice.CompanyId))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                RealmId = "123"
            });

        var clientMock = new Mock<IQuickBooksInvoiceClient>();
        clientMock.Setup(c => c.FindCustomerByEmailAsync(It.IsAny<ServiceContext>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((QboCustomer?)null);
        clientMock.Setup(c => c.FindCustomerByDisplayNameAsync(It.IsAny<ServiceContext>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new QboCustomer { Id = "cust-1", DisplayName = invoice.CustomerName });

        var invocationCount = 0;
        clientMock.Setup(c => c.CreateInvoiceAsync(It.IsAny<ServiceContext>(), It.IsAny<QboInvoice>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns<ServiceContext, QboInvoice, System.Threading.CancellationToken>((_, _, _) =>
            {
                invocationCount++;
                if (invocationCount == 1)
                {
                    return Task.FromException<QboInvoice>(new HttpRequestException("rate limit"));
                }

                return Task.FromResult(new QboInvoice { Id = "INV-RETRY" });
            });

        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncService>>();
        var service = new QuickBooksInvoiceSyncService(context, authServiceMock.Object, clientMock.Object, logger);

        var result = await service.SyncInvoiceToQBOAsync(invoice.Id);

        Assert.True(result.Success);
        Assert.Equal("INV-RETRY", result.QboInvoiceId);

        var log = await context.AccountingSyncLogs.SingleAsync(l => l.EntityId == invoice.Id.ToString());
        Assert.Equal(1, log.RetryCount);
        Assert.Equal(SyncStatus.Success, log.SyncStatus);
    }

    private static async Task<Invoice> SeedInvoiceAsync(CaskrDbContext context)
    {
        var company = new Company
        {
            CompanyName = "Test Co",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = company.Id,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.ChartOfAccountsMappings.Add(new ChartOfAccountsMapping
        {
            CompanyId = company.Id,
            CaskrAccountType = CaskrAccountType.FinishedGoods,
            QboAccountId = "acct-1",
            QboAccountName = "Finished Goods",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            CompanyId = company.Id,
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
            SubtotalAmount = 100m,
            TotalAmount = 106m,
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
                    Amount = 6m
                }
            }
        };

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        return invoice;
    }
}
