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
        var expected = new[] { new Status { Id = (int)StatusType.ResearchAndDevelopment } };
        _repo.Setup(r => r.GetStatusesAsync()).ReturnsAsync(expected);

        var result = await _service.GetStatusesAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetStatusAsync_DelegatesToRepository()
    {
        var expected = new Status { Id = (int)StatusType.AssetCreation };
        _repo.Setup(r => r.GetStatusAsync((int)StatusType.AssetCreation)).ReturnsAsync(expected);

        var result = await _service.GetStatusAsync((int)StatusType.AssetCreation);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddStatusAsync_DelegatesToRepository()
    {
        var status = new Status { Id = (int)StatusType.TtbApproval };
        _repo.Setup(r => r.AddStatusAsync(status)).ReturnsAsync(status);

        var result = await _service.AddStatusAsync(status);

        Assert.Equal(status, result);
    }

    [Fact]
    public async Task UpdateStatusAsync_DelegatesToRepository()
    {
        var status = new Status { Id = (int)StatusType.Ordering };
        _repo.Setup(r => r.UpdateStatusAsync(status)).ReturnsAsync(status);

        var result = await _service.UpdateStatusAsync(status);

        Assert.Equal(status, result);
    }

    [Fact]
    public async Task DeleteStatusAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteStatusAsync((int)StatusType.OhlqListing)).Returns(Task.CompletedTask);

        await _service.DeleteStatusAsync((int)StatusType.OhlqListing);

        _repo.Verify(r => r.DeleteStatusAsync((int)StatusType.OhlqListing), Times.Once);
    }
}
