using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Caskr.Server.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsersService> _usersService = new();
    private readonly Mock<IKeycloakClient> _keycloak = new();
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        var settings = new Dictionary<string, string>
        {
            {"Jwt:Key", "testing-key-12345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        _authService = new AuthService(_usersService.Object, configuration, _keycloak.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenUserExists()
    {
        var user = new User { Id = 1, Email = "a@b.com" };
        _usersService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(user);

        _keycloak.Setup(k => k.GetTokenAsync("a@b.com", "pass")).ReturnsAsync("kc-token");

        var token = await _authService.LoginAsync("a@b.com", "pass");

        Assert.NotNull(token);
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserMissing()
    {
        _usersService.Setup(s => s.GetUserByEmailAsync("missing@b.com")).ReturnsAsync((User?)null);
        _keycloak.Setup(k => k.GetTokenAsync("missing@b.com", "pass")).ReturnsAsync("kc-token");

        var token = await _authService.LoginAsync("missing@b.com", "pass");

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenKeycloakFails()
    {
        var user = new User { Id = 1, Email = "a@b.com" };
        _usersService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(user);
        _keycloak.Setup(k => k.GetTokenAsync("a@b.com", "bad")).ReturnsAsync((string?)null);

        var token = await _authService.LoginAsync("a@b.com", "bad");

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_SuperAdmin_BypassesKeycloak()
    {
        var user = new User { Id = 1, Email = "super@admin.com", UserTypeId = (int)UserType.SuperAdmin };
        _usersService.Setup(s => s.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);

        var token = await _authService.LoginAsync(user.Email, "any-password");

        Assert.False(string.IsNullOrEmpty(token));
        _keycloak.Verify(k => k.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
