using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Caskr.server.Models;
using Intuit.Ipp.OAuth2PlatformClient;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksAuthServiceTests
{
    [Fact]
    public async Task HandleCallbackAsync_PersistsEncryptedTokens()
    {
        await using var context = CreateDbContext();
        var clientMock = new Mock<IQuickBooksOAuthClient>(MockBehavior.Strict);
        var factoryMock = new Mock<IQuickBooksOAuthClientFactory>(MockBehavior.Strict);
        var service = CreateService(context, clientMock, factoryMock);

        clientMock.Setup(c => c.ExchangeCodeForTokenAsync("code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTokenResponse("access-token", "refresh-token", 3600, 7776000));

        var response = await service.HandleCallbackAsync("code", "realm", 5);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal("realm", response.RealmId);

        var integration = await context.AccountingIntegrations.SingleAsync();
        Assert.Equal(5, integration.CompanyId);
        Assert.True(integration.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(integration.AccessTokenEncrypted));
        Assert.NotEqual("access-token", integration.AccessTokenEncrypted);
        Assert.NotEqual("refresh-token", integration.RefreshTokenEncrypted);

        clientMock.VerifyAll();
        factoryMock.VerifyAll();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenExpired_RefreshesTokens()
    {
        await using var context = CreateDbContext();
        var clientMock = new Mock<IQuickBooksOAuthClient>(MockBehavior.Strict);
        var factoryMock = new Mock<IQuickBooksOAuthClientFactory>(MockBehavior.Strict);
        var service = CreateService(context, clientMock, factoryMock);

        clientMock.Setup(c => c.ExchangeCodeForTokenAsync("code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTokenResponse("initial-access", "initial-refresh", 3600, 7776000));

        await service.HandleCallbackAsync("code", "realm", 9);
        var integration = await context.AccountingIntegrations.SingleAsync();
        var previousToken = integration.AccessTokenEncrypted;
        integration.UpdatedAt = DateTime.UtcNow.AddHours(-2);
        await context.SaveChangesAsync();

        clientMock.Setup(c => c.RefreshTokenAsync("initial-refresh", "realm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTokenResponse("new-access", "new-refresh", 3600, 7776000));

        var refreshed = await service.RefreshTokenAsync(9);

        Assert.Equal("new-access", refreshed.AccessToken);
        Assert.Equal("new-refresh", refreshed.RefreshToken);
        clientMock.Verify(c => c.RefreshTokenAsync("initial-refresh", "realm", It.IsAny<CancellationToken>()), Times.Once);

        var updated = await context.AccountingIntegrations.SingleAsync();
        Assert.NotEqual(previousToken, updated.AccessTokenEncrypted);

        clientMock.VerifyAll();
        factoryMock.VerifyAll();
    }

    private static QuickBooksAuthService CreateService(CaskrDbContext context, Mock<IQuickBooksOAuthClient> clientMock, Mock<IQuickBooksOAuthClientFactory> factoryMock)
    {
        var settings = new Dictionary<string, string?>
        {
            ["QuickBooks:ClientId"] = "client-id",
            ["QuickBooks:ClientSecret"] = "client-secret",
            ["QuickBooks:RedirectUri"] = "https://example.com/callback",
            ["QuickBooks:Environment"] = "sandbox"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        factoryMock.Setup(f => f.Create("client-id", "client-secret", "https://example.com/callback", "sandbox"))
            .Returns(clientMock.Object);

        var protector = new EphemeralDataProtectionProvider();
        var logger = NullLogger<QuickBooksAuthService>.Instance;
        return new QuickBooksAuthService(configuration, context, protector, logger, factoryMock.Object);
    }

    private static CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private static TokenResponse CreateTokenResponse(string accessToken, string refreshToken, long expiresIn, long refreshExpires)
    {
        var payload = new
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            token_type = "bearer",
            expires_in = expiresIn,
            x_refresh_token_expires_in = refreshExpires
        };
        var json = JsonSerializer.Serialize(payload);
        return new TokenResponse(json);
    }
}
