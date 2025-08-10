using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;
using System.Linq;
using System;

namespace Caskr.Server.Tests;

public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _repo = new();
    private readonly Mock<IUsersRepository> _usersRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly IOrdersService _service;

    public OrdersServiceTests()
    {
        _service = new OrdersService(_repo.Object, _usersRepo.Object, _email.Object);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new Order { Id = 1, StatusId = (int)StatusType.ResearchAndDevelopment } };
        _repo.Setup(r => r.GetOrdersAsync()).ReturnsAsync(expected);

        var result = await _service.GetOrdersAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetOrderAsync_DelegatesToRepository()
    {
        var expected = new Order { Id = 2, StatusId = (int)StatusType.AssetCreation };
        _repo.Setup(r => r.GetOrderAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetOrderAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddOrderAsync_DelegatesToRepository()
    {
        var order = new Order { Id = 3, StatusId = (int)StatusType.TtbApproval };
        _repo.Setup(r => r.AddOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.AddOrderAsync(order);

        Assert.Equal(order, result);
    }

    [Fact]
    public async Task UpdateOrderAsync_DelegatesToRepository()
    {
        var order = new Order { Id = 4, OwnerId = 1, StatusId = (int)StatusType.Ordering };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 4, OwnerId = 1, StatusId = (int)StatusType.AssetCreation });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.UpdateOrderAsync(order);

        Assert.Equal(order, result);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderAsync_StatusChangedToTtbApproval_SendsEmail()
    {
        var order = new Order { Id = 6, OwnerId = 2, StatusId = (int)StatusType.TtbApproval, Name = "Order" };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 6, OwnerId = 2, StatusId = (int)StatusType.AssetCreation });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);
        var user = new User { Id = 2, Email = "owner@example.com", Name = "Owner" };
        _usersRepo.Setup(r => r.GetUserAsync(order.OwnerId)).ReturnsAsync(user);

        await _service.UpdateOrderAsync(order);

        _email.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetOutstandingTasksAsync_ReturnsOutstandingTasks()
    {
        var orderId = 7;
        var status = new Status
        {
            Id = 1,
            StatusTasks = new List<StatusTask>
            {
                new StatusTask { Id = 1, StatusId = 1, Name = "Task1" },
                new StatusTask { Id = 2, StatusId = 1, Name = "Task2" }
            }
        };
        var order = new Order
        {
            Id = orderId,
            StatusId = 1,
            Status = status,
            Tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, OrderId = orderId, Name = "Task1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow }
            }
        };
        _repo.Setup(r => r.GetOrderWithTasksAsync(orderId)).ReturnsAsync(order);

        var result = await _service.GetOutstandingTasksAsync(orderId);
        Assert.NotNull(result);
        var task = Assert.Single(result);
        Assert.Equal(2, task.Id);
        Assert.Equal(1, task.StatusId);
        Assert.Equal("Task2", task.Name);
    }

    [Fact]
    public async Task DeleteOrderAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteOrderAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteOrderAsync(5);

        _repo.Verify(r => r.DeleteOrderAsync(5), Times.Once);
    }
}
