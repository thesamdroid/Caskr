using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksDataServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IQuickBooksAuthService> _authService = new();
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
        _authService
            .Setup(s => s.RefreshTokenAsync(100))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                RealmId = "12345",
                ExpiresIn = 3600
            });

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
            _context,
            _authService.Object,
            _queryClient.Object,
            NullLogger<QuickBooksDataService>.Instance);

        var first = await service.GetChartOfAccountsAsync(100);
        var second = await service.GetChartOfAccountsAsync(100);

        Assert.Single(first);
        Assert.Equal("Cash", first[0].Name);
        Assert.Equal(first, second);
        _authService.Verify(s => s.RefreshTokenAsync(100), Times.Once);
        _queryClient.Verify(c => c.ExecuteActiveAccountQuery(It.IsAny<ServiceContext>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetChartOfAccountsAsync_NoIntegration_Throws()
    {
        var service = new QuickBooksDataService(
            _memoryCache,
            _context,
            _authService.Object,
            _queryClient.Object,
            NullLogger<QuickBooksDataService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetChartOfAccountsAsync(999));
    }

    public void Dispose()
    {
        _context.Dispose();
        _memoryCache.Dispose();
    }
}
