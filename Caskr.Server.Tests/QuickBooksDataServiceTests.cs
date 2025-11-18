using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksDataServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IQuickBooksIntegrationContextFactory> _contextFactory = new();
    private readonly Mock<IQuickBooksAccountQueryClient> _queryClient = new();
    private readonly CaskrDbContext _context;

    public QuickBooksDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new CaskrDbContext(options);
        _context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = 100,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "12345",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public async System.Threading.Tasks.Task GetChartOfAccountsAsync_UsesCacheAfterFirstCall()
    {
        SetupContextFactory(100);

        _queryClient
            .Setup(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()))
            .Returns(new List<Account>
            {
                new()
                {
                    Id = "77",
                    Name = "Cash",
                    AccountSubType = "CashOnHand",
                    AccountType = AccountTypeEnum.Bank,
                    AccountTypeSpecified = true,
                    Active = true,
                    ActiveSpecified = true
                }
            });

        var service = new QuickBooksDataService(
            _memoryCache,
            _contextFactory.Object,
            _queryClient.Object,
            NullLogger<QuickBooksDataService>.Instance);

        var first = await service.GetChartOfAccountsAsync(100);
        var second = await service.GetChartOfAccountsAsync(100);

        Assert.Single(first);
        Assert.Equal("Cash", first[0].Name);
        Assert.Equal(first, second);
        _contextFactory.Verify(f => f.CreateAsync(100, It.IsAny<CancellationToken>()), Times.Once);
        _queryClient.Verify(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetChartOfAccountsAsync_BypassesCacheWhenRequested()
    {
        SetupContextFactory(100);

        _queryClient
            .SetupSequence(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()))
            .Returns(new List<Account>
            {
                new()
                {
                    Id = "77",
                    Name = "Cash",
                    AccountSubType = "CashOnHand",
                    AccountType = AccountTypeEnum.Bank,
                    AccountTypeSpecified = true,
                    Active = true,
                    ActiveSpecified = true
                }
            })
            .Returns(new List<Account>
            {
                new()
                {
                    Id = "78",
                    Name = "Revenue",
                    AccountSubType = "SalesOfProductIncome",
                    AccountType = AccountTypeEnum.Income,
                    AccountTypeSpecified = true,
                    Active = true,
                    ActiveSpecified = true
                }
            });

        var service = new QuickBooksDataService(
            _memoryCache,
            _contextFactory.Object,
            _queryClient.Object,
            NullLogger<QuickBooksDataService>.Instance);

        var first = await service.GetChartOfAccountsAsync(100);
        var refreshed = await service.GetChartOfAccountsAsync(100, true);

        Assert.Equal("Cash", first[0].Name);
        Assert.Equal("Revenue", refreshed[0].Name);
        _contextFactory.Verify(f => f.CreateAsync(100, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _queryClient.Verify(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()), Times.Exactly(2));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetChartOfAccountsAsync_NoIntegration_Throws()
    {
        _contextFactory
            .Setup(f => f.CreateAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No integration"));

        var service = new QuickBooksDataService(
            _memoryCache,
            _contextFactory.Object,
            _queryClient.Object,
            NullLogger<QuickBooksDataService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetChartOfAccountsAsync(999));
    }

    public void Dispose()
    {
        _context.Dispose();
        _memoryCache.Dispose();
    }

    private void SetupContextFactory(int companyId)
    {
        var validator = new OAuth2RequestValidator("token");
        var context = new ServiceContext("12345", IntuitServicesType.QBO, validator);
        _contextFactory
            .Setup(f => f.CreateAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuickBooksIntegrationContext(companyId, "12345", context));
    }
}
