using System;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksIntegrationContextFactoryTests
{
    [Fact]
    public async Task CreateAsync_ReturnsServiceContext_WhenIntegrationActive()
    {
        await using var context = CreateDbContext();
        var integration = await SeedIntegrationAsync(context);
        var authService = new Mock<IQuickBooksAuthService>();
        authService.Setup(s => s.RefreshTokenAsync(integration.CompanyId))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                RealmId = "realm-override",
                ExpiresIn = 3600
            });

        var factory = new QuickBooksIntegrationContextFactory(
            context,
            authService.Object,
            NullLogger<QuickBooksIntegrationContextFactory>.Instance);

        var result = await factory.CreateAsync(integration.CompanyId);

        Assert.Equal(integration.CompanyId, result.CompanyId);
        Assert.Equal("realm-override", result.RealmId);
        Assert.NotNull(result.ServiceContext);
        authService.Verify(s => s.RefreshTokenAsync(integration.CompanyId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesStoredRealm_WhenTokenDoesNotIncludeRealm()
    {
        await using var context = CreateDbContext();
        var integration = await SeedIntegrationAsync(context);
        var authService = new Mock<IQuickBooksAuthService>();
        authService.Setup(s => s.RefreshTokenAsync(integration.CompanyId))
            .ReturnsAsync(new OAuthTokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                RealmId = string.Empty,
                ExpiresIn = 3600
            });

        var factory = new QuickBooksIntegrationContextFactory(
            context,
            authService.Object,
            NullLogger<QuickBooksIntegrationContextFactory>.Instance);

        var result = await factory.CreateAsync(integration.CompanyId);

        Assert.Equal(integration.RealmId, result.RealmId);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenIntegrationMissing()
    {
        await using var context = CreateDbContext();
        var authService = new Mock<IQuickBooksAuthService>();
        var factory = new QuickBooksIntegrationContextFactory(
            context,
            authService.Object,
            NullLogger<QuickBooksIntegrationContextFactory>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync(999));
    }

    private static async Task<AccountingIntegration> SeedIntegrationAsync(CaskrDbContext context)
    {
        var integration = new AccountingIntegration
        {
            CompanyId = 42,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm-default",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.AccountingIntegrations.Add(integration);
        await context.SaveChangesAsync();
        return integration;
    }

    private static CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }
}
