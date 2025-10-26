using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Caskr.Server.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsLoginResponse_WhenCredentialsValid()
    {
        using var context = CreateDbContext();
        var company = new Company { Id = 1, CompanyName = "Test Co" };
        context.Companies.Add(company);

        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "a@b.com",
            CompanyId = company.Id,
            Company = company,
            UserTypeId = 2,
            IsActive = true,
            KeycloakUserId = "kc-123"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = CreateAuthService(context, (_, _) => Task.FromResult(CreateTokenResponse()));

        var response = await authService.LoginAsync("a@b.com", "password");

        Assert.Equal("token", response.Token);
        Assert.Equal("refresh", response.RefreshToken);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.Name, response.User.Name);
        Assert.Equal(user.Email, response.User.Email);
        Assert.Equal(user.CompanyId, response.User.CompanyId);
        Assert.Equal(company.CompanyName, response.User.CompanyName);
        Assert.Equal(user.UserTypeId, response.User.UserTypeId);
        Assert.NotNull(context.Users.Single().LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenUserDoesNotExist()
    {
        using var context = CreateDbContext();

        var authService = CreateAuthService(context, (_, _) => Task.FromResult(CreateTokenResponse()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync("missing@b.com", "password"));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenKeycloakRejectsCredentials()
    {
        using var context = CreateDbContext();
        var company = new Company { Id = 1, CompanyName = "Test Co" };
        context.Companies.Add(company);
        context.Users.Add(new User
        {
            Id = 1,
            Name = "Test User",
            Email = "a@b.com",
            CompanyId = company.Id,
            Company = company,
            UserTypeId = 2,
            IsActive = true,
            KeycloakUserId = "kc-123"
        });
        await context.SaveChangesAsync();

        var authService = CreateAuthService(context, (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync("a@b.com", "bad"));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenUserInactive()
    {
        using var context = CreateDbContext();
        var company = new Company { Id = 1, CompanyName = "Test Co" };
        context.Companies.Add(company);
        context.Users.Add(new User
        {
            Id = 1,
            Name = "Inactive User",
            Email = "inactive@b.com",
            CompanyId = company.Id,
            Company = company,
            UserTypeId = 2,
            IsActive = false,
            KeycloakUserId = "kc-456"
        });
        await context.SaveChangesAsync();

        var authService = CreateAuthService(context, (_, _) => Task.FromResult(CreateTokenResponse()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync("inactive@b.com", "password"));
    }

    private static CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private static AuthService CreateAuthService(
        CaskrDbContext context,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:BaseUrl"] = "http://localhost",
                ["Keycloak:Realm"] = "caskr",
                ["Keycloak:ClientId"] = "caskr-client",
                ["Keycloak:ClientSecret"] = "secret",
                ["Keycloak:AdminUsername"] = "admin",
                ["Keycloak:AdminPassword"] = "admin"
            })
            .Build();

        var httpClientFactory = new StubHttpClientFactory(new StubHttpMessageHandler(handler));

        return new AuthService(context, httpClientFactory, configuration, NullLogger<AuthService>.Instance);
    }

    private static HttpResponseMessage CreateTokenResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"access_token\":\"token\",\"refresh_token\":\"refresh\",\"expires_in\":300,\"refresh_expires_in\":3600,\"token_type\":\"Bearer\"}",
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new HttpClient(_handler, disposeHandler: false);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
