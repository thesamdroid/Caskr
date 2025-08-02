using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _repo = new();
    private readonly IOrdersService _service;

    public OrdersServiceTests()
    {
        _service = new OrdersService(_repo.Object);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new Order { Id = 1, StatusId = StatusType.ResearchAndDevelopment } };
        _repo.Setup(r => r.GetOrdersAsync()).ReturnsAsync(expected);

        var result = await _service.GetOrdersAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetOrderAsync_DelegatesToRepository()
    {
        var expected = new Order { Id = 2, StatusId = StatusType.AssetCreation };
        _repo.Setup(r => r.GetOrderAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetOrderAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddOrderAsync_DelegatesToRepository()
    {
        var order = new Order { Id = 3, StatusId = StatusType.TtbApproval };
        _repo.Setup(r => r.AddOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.AddOrderAsync(order);

        Assert.Equal(order, result);
    }

    [Fact]
    public async Task UpdateOrderAsync_DelegatesToRepository()
    {
        var order = new Order { Id = 4, StatusId = StatusType.Ordering };
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.UpdateOrderAsync(order);

        Assert.Equal(order, result);
    }

    [Fact]
    public async Task DeleteOrderAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteOrderAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteOrderAsync(5);

        _repo.Verify(r => r.DeleteOrderAsync(5), Times.Once);
    }
}
