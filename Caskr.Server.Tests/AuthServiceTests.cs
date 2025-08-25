using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Caskr.Server.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsersService> _usersService = new();
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
        _authService = new AuthService(_usersService.Object, configuration);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenUserExists()
    {
        var user = new User { Id = 1, Email = "a@b.com" };
        _usersService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(user);

        var token = await _authService.LoginAsync("a@b.com");

        Assert.NotNull(token);
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserMissing()
    {
        _usersService.Setup(s => s.GetUserByEmailAsync("missing@b.com")).ReturnsAsync((User?)null);

        var token = await _authService.LoginAsync("missing@b.com");

        Assert.Null(token);
    }
}
