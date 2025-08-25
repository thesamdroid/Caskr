using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class UsersServiceTests
{
    private readonly Mock<IUsersRepository> _repo = new();
    private readonly IUsersService _service;

    public UsersServiceTests()
    {
        _service = new UsersService(_repo.Object);
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
    public async Task GetUserAsync_DelegatesToRepository()
    {
        var expected = new User { Id = 2 };
        _repo.Setup(r => r.GetUserAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetUserAsync(2);

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
        var user = new User { Id = 3 };
        _repo.Setup(r => r.AddUserAsync(user)).ReturnsAsync(user);

        var result = await _service.AddUserAsync(user);

        Assert.Equal(user, result);
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
