using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using UserTypeEnum = Caskr.server.UserType;
using Moq;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Caskr.Server.Tests;

public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _repo = new();
    private readonly Mock<IUsersRepository> _usersRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly IOrdersService _service;

    public OrdersServiceTests()
    {
        _repo.Setup(r => r.AddTasksForStatusAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _service = new OrdersService(_repo.Object, _usersRepo.Object, _email.Object);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new Order { Id = 1, StatusId = (int)StatusType.ResearchAndDevelopment, SpiritTypeId = 1 } };
        _repo.Setup(r => r.GetOrdersAsync()).ReturnsAsync(expected);

        var result = await _service.GetOrdersAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetOrdersForOwnerAsync_NonAdminUser_ReturnsOwnerOrders()
    {
        var user = new User { Id = 5, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Distiller };
        var expected = new[] { new Order { Id = 9, OwnerId = 5, StatusId = (int)StatusType.ResearchAndDevelopment, SpiritTypeId = 1 } };
        _usersRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _repo.Setup(r => r.GetOrdersForOwnerAsync(5)).ReturnsAsync(expected);

        var result = await _service.GetOrdersForOwnerAsync(5);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetOrdersForOwnerAsync_AdminUser_ReturnsCompanyOrders()
    {
        var user = new User { Id = 7, CompanyId = 3, UserTypeId = (int)UserTypeEnum.Admin };
        var expected = new[] { new Order { Id = 1, CompanyId = 3, OwnerId = 2, StatusId = (int)StatusType.ResearchAndDevelopment, SpiritTypeId = 1 } };
        _usersRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _repo.Setup(r => r.GetOrdersForCompanyAsync(3)).ReturnsAsync(expected);

        var result = await _service.GetOrdersForOwnerAsync(7);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetOrderAsync_DelegatesToRepository()
    {
        var expected = new Order { Id = 2, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetOrderAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddOrderAsync_DelegatesToRepository()
    {
        var order = new Order { Id = 3, StatusId = (int)StatusType.TtbApproval, SpiritTypeId = 1 };
        _repo.Setup(r => r.AddOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.AddOrderAsync(order);

        Assert.Equal(order, result);
    }

    [Fact]
    public async Task UpdateOrderAsync_StatusChanged_CreatesTasks()
    {
        var order = new Order { Id = 4, OwnerId = 1, StatusId = (int)StatusType.Ordering, SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 4, OwnerId = 1, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.UpdateOrderAsync(order);

        Assert.Equal(order, result);
        _repo.Verify(r => r.AddTasksForStatusAsync(order.Id, order.StatusId), Times.Once);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderAsync_StatusChangedToTtbApproval_SendsEmail()
    {
        var order = new Order { Id = 6, OwnerId = 2, StatusId = (int)StatusType.TtbApproval, Name = "Order", SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 6, OwnerId = 2, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);
        var user = new User { Id = 2, Email = "owner@example.com", Name = "Owner" };
        _usersRepo.Setup(r => r.GetUserByIdAsync(order.OwnerId)).ReturnsAsync(user);

        await _service.UpdateOrderAsync(order);

        _repo.Verify(r => r.AddTasksForStatusAsync(order.Id, order.StatusId), Times.Once);
        _email.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_StatusUnchanged_DoesNotCreateTasks()
    {
        var order = new Order { Id = 8, OwnerId = 1, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 8, OwnerId = 1, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);

        var result = await _service.UpdateOrderAsync(order);

        Assert.Equal(order, result);
        _repo.Verify(r => r.AddTasksForStatusAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
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
            SpiritTypeId = 1,
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
    public async Task GetOutstandingTasksAsync_TaskCreatedButNotCompleted_ReturnsTask()
    {
        var orderId = 15;
        var status = new Status
        {
            Id = 1,
            StatusTasks = new List<StatusTask>
            {
                new() { Id = 1, StatusId = 1, Name = "Task1" }
            }
        };
        var order = new Order
        {
            Id = orderId,
            StatusId = 1,
            SpiritTypeId = 1,
            Status = status,
            Tasks = new List<TaskItem>
            {
                new() { Id = 1, OrderId = orderId, Name = "Task1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CompletedAt = null }
            }
        };
        _repo.Setup(r => r.GetOrderWithTasksAsync(orderId)).ReturnsAsync(order);

        var result = await _service.GetOutstandingTasksAsync(orderId);

        var outstanding = Assert.Single(result);
        Assert.Equal("Task1", outstanding.Name);
    }

    [Fact]
    public async Task GetOutstandingTasksAsync_OrderNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetOrderWithTasksAsync(10)).ReturnsAsync((Order?)null);

        var result = await _service.GetOutstandingTasksAsync(10);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOutstandingTasksAsync_StatusTasksMissing_ReturnsEmpty()
    {
        var orderId = 12;
        var order = new Order
        {
            Id = orderId,
            StatusId = 1,
            SpiritTypeId = 1,
            Status = new Status { Id = 1, StatusTasks = null },
            Tasks = new List<TaskItem>()
        };
        _repo.Setup(r => r.GetOrderWithTasksAsync(orderId)).ReturnsAsync(order);

        var result = await _service.GetOutstandingTasksAsync(orderId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOutstandingTasksAsync_OrderTasksMissing_HandlesGracefully()
    {
        var orderId = 13;
        var order = new Order
        {
            Id = orderId,
            StatusId = 1,
            SpiritTypeId = 1,
            Status = new Status
            {
                Id = 1,
                StatusTasks = new List<StatusTask>
                {
                    new() { Id = 1, StatusId = 1, Name = "Task1" }
                }
            },
            Tasks = null
        };
        _repo.Setup(r => r.GetOrderWithTasksAsync(orderId)).ReturnsAsync(order);

        var result = await _service.GetOutstandingTasksAsync(orderId);

        var outstanding = Assert.Single(result);
        Assert.Equal("Task1", outstanding.Name);
    }

    [Fact]
    public async Task GetOutstandingTasksAsync_ComparesTaskNamesCaseInsensitive()
    {
        var orderId = 14;
        var order = new Order
        {
            Id = orderId,
            StatusId = 1,
            SpiritTypeId = 1,
            Status = new Status
            {
                Id = 1,
                StatusTasks = new List<StatusTask>
                {
                    new() { Id = 1, StatusId = 1, Name = "TaskOne" },
                    new() { Id = 2, StatusId = 1, Name = "TaskTwo" }
                }
            },
            Tasks = new List<TaskItem>
            {
                new() { Id = 1, OrderId = orderId, Name = "taskone" }
            }
        };
        _repo.Setup(r => r.GetOrderWithTasksAsync(orderId)).ReturnsAsync(order);

        var result = await _service.GetOutstandingTasksAsync(orderId);

        var outstanding = Assert.Single(result);
        Assert.Equal("TaskTwo", outstanding.Name);
    }

    [Fact]
    public async Task GetOrdersForOwnerAsync_UserNotFound_ReturnsOwnerOrders()
    {
        var expected = new[] { new Order { Id = 9, OwnerId = 5, StatusId = (int)StatusType.ResearchAndDevelopment, SpiritTypeId = 1 } };
        _usersRepo.Setup(r => r.GetUserByIdAsync(5)).ReturnsAsync((User?)null);
        _repo.Setup(r => r.GetOrdersForOwnerAsync(5)).ReturnsAsync(expected);

        var result = await _service.GetOrdersForOwnerAsync(5);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateOrderAsync_StatusChangedToTtbApproval_UserNotFound_NoEmail()
    {
        var order = new Order { Id = 10, OwnerId = 3, StatusId = (int)StatusType.TtbApproval, Name = "Order", SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync(new Order { Id = 10, OwnerId = 3, StatusId = (int)StatusType.AssetCreation, SpiritTypeId = 1 });
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);
        _usersRepo.Setup(r => r.GetUserByIdAsync(order.OwnerId)).ReturnsAsync((User?)null);

        await _service.UpdateOrderAsync(order);

        _repo.Verify(r => r.AddTasksForStatusAsync(order.Id, order.StatusId), Times.Once);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderAsync_ExistingOrderNull_SendsEmailWhenTtbApproval()
    {
        var order = new Order { Id = 11, OwnerId = 4, StatusId = (int)StatusType.TtbApproval, Name = "Order2", SpiritTypeId = 1 };
        _repo.Setup(r => r.GetOrderAsync(order.Id)).ReturnsAsync((Order?)null);
        _repo.Setup(r => r.UpdateOrderAsync(order)).ReturnsAsync(order);
        var user = new User { Id = 4, Email = "owner@example.com", Name = "Owner" };
        _usersRepo.Setup(r => r.GetUserByIdAsync(order.OwnerId)).ReturnsAsync(user);

        await _service.UpdateOrderAsync(order);

        _repo.Verify(r => r.AddTasksForStatusAsync(order.Id, order.StatusId), Times.Once);
        _email.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOrderAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteOrderAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteOrderAsync(5);

        _repo.Verify(r => r.DeleteOrderAsync(5), Times.Once);
    }
}
