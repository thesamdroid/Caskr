using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class UsersServiceTests
{
    private readonly Mock<IUsersRepository> _repo = new();
    private readonly Mock<IKeycloakClient> _kcClient = new();
    private readonly IUsersService _service;

    public UsersServiceTests()
    {
        _service = new UsersService(_repo.Object, _kcClient.Object);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new User { Id = 1 } };
        _repo.Setup(r => r.GetUsersAsync()).ReturnsAsync(expected);

        var result = await _service.GetUsersAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetUserByIdAsync_DelegatesToRepository()
    {
        var expected = new User { Id = 2 };
        _repo.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetUserByIdAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_DelegatesToRepository()
    {
        var expected = new User { Id = 6, Email = "test@example.com" };
        _repo.Setup(r => r.GetUserByEmailAsync("test@example.com")).ReturnsAsync(expected);

        var result = await _service.GetUserByEmailAsync("test@example.com");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddUserAsync_DelegatesToRepository()
    {
        var user = new User { Id = 3, TemporaryPassword = "pass" };
        _repo.Setup(r => r.AddUserAsync(user)).ReturnsAsync(user);
        _kcClient.Setup(k => k.CreateUserAsync(user, "pass")).Returns(Task.CompletedTask);

        var result = await _service.AddUserAsync(user);

        Assert.Equal(user, result);
        _kcClient.Verify(k => k.CreateUserAsync(user, "pass"), Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_WhenUserExists_Throws()
    {
        var user = new User { Id = 7, Email = "exists@example.com", TemporaryPassword = "pass" };
        _repo.Setup(r => r.GetUserByEmailAsync("exists@example.com")).ReturnsAsync(new User());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddUserAsync(user));
        _kcClient.Verify(k => k.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_NullUser_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddUserAsync(null));
    }

    [Fact]
    public async Task AddUserAsync_KeycloakThrows_Propagates()
    {
        var user = new User { Id = 8, TemporaryPassword = "pass" };
        _kcClient.Setup(k => k.CreateUserAsync(user, "pass")).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddUserAsync(user));
        _repo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_DelegatesToRepository()
    {
        var user = new User { Id = 4 };
        _repo.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);

        var result = await _service.UpdateUserAsync(user);

        Assert.Equal(user, result);
    }

    [Fact]
    public async Task DeleteUserAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteUserAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteUserAsync(5);

        _repo.Verify(r => r.DeleteUserAsync(5), Times.Once);
    }
}
