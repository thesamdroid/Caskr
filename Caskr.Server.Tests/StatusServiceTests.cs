using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class StatusServiceTests
{
    private readonly Mock<IStatusRepository> _repo = new();
    private readonly IStatusService _service;

    public StatusServiceTests()
    {
        _service = new StatusService(_repo.Object);
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new Status { Id = 1 } };
        _repo.Setup(r => r.GetStatusesAsync()).ReturnsAsync(expected);

        var result = await _service.GetStatusesAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetStatusAsync_DelegatesToRepository()
    {
        var expected = new Status { Id = 2 };
        _repo.Setup(r => r.GetStatusAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetStatusAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddStatusAsync_DelegatesToRepository()
    {
        var status = new Status { Id = 3 };
        _repo.Setup(r => r.AddStatusAsync(status)).ReturnsAsync(status);

        var result = await _service.AddStatusAsync(status);

        Assert.Equal(status, result);
    }

    [Fact]
    public async Task UpdateStatusAsync_DelegatesToRepository()
    {
        var status = new Status { Id = 4 };
        _repo.Setup(r => r.UpdateStatusAsync(status)).ReturnsAsync(status);

        var result = await _service.UpdateStatusAsync(status);

        Assert.Equal(status, result);
    }

    [Fact]
    public async Task DeleteStatusAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteStatusAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteStatusAsync(5);

        _repo.Verify(r => r.DeleteStatusAsync(5), Times.Once);
    }
}
