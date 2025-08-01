using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class UserTypesServiceTests
{
    private readonly Mock<IUserTypesRepository> _repo = new();
    private readonly IUserTypesService _service;

    public UserTypesServiceTests()
    {
        _service = new UserTypesService(_repo.Object);
    }

    [Fact]
    public async Task GetUserTypesAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new UserType { Id = 1 } };
        _repo.Setup(r => r.GetUserTypesAsync()).ReturnsAsync(expected);

        var result = await _service.GetUserTypesAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetUserTypeAsync_DelegatesToRepository()
    {
        var expected = new UserType { Id = 2 };
        _repo.Setup(r => r.GetUserTypeAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetUserTypeAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddUserTypeAsync_DelegatesToRepository()
    {
        var userType = new UserType { Id = 3 };
        _repo.Setup(r => r.AddUserTypeAsync(userType)).ReturnsAsync(userType);

        var result = await _service.AddUserTypeAsync(userType);

        Assert.Equal(userType, result);
    }

    [Fact]
    public async Task UpdateUserTypeAsync_DelegatesToRepository()
    {
        var userType = new UserType { Id = 4 };
        _repo.Setup(r => r.UpdateUserTypeAsync(userType)).ReturnsAsync(userType);

        var result = await _service.UpdateUserTypeAsync(userType);

        Assert.Equal(userType, result);
    }

    [Fact]
    public async Task DeleteUserTypeAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteUserTypeAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteUserTypeAsync(5);

        _repo.Verify(r => r.DeleteUserTypeAsync(5), Times.Once);
    }
}
