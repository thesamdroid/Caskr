using System.Collections.Generic;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Caskr.Server.Tests;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _controller = new TasksController(_taskService.Object, NullLogger<TasksController>.Instance);
    }

    [Fact]
    public async Task GetTasksByOrder_ReturnsTasksFromService()
    {
        var orderId = 3;
        var tasks = new[]
        {
            new OrderTask { Id = 1, OrderId = orderId, Name = "Prepare barrels" },
            new OrderTask { Id = 2, OrderId = orderId, Name = "Schedule tasting" }
        };
        _taskService.Setup(s => s.GetTasksByOrderIdAsync(orderId)).ReturnsAsync(tasks);

        var result = await _controller.GetTasksByOrder(orderId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<OrderTask>>(ok.Value);
        Assert.Collection(payload,
            first => Assert.Equal(tasks[0].Id, first.Id),
            second => Assert.Equal(tasks[1].Id, second.Id));
    }

    [Fact]
    public async Task GetTasksByOrderRouteAlias_DelegatesToPrimaryAction()
    {
        var orderId = 11;
        var tasks = new[]
        {
            new OrderTask { Id = 9, OrderId = orderId, Name = "Confirm shipment" }
        };
        _taskService.Setup(s => s.GetTasksByOrderIdAsync(orderId)).ReturnsAsync(tasks);

        var result = await _controller.GetTasksByOrderRouteAlias(orderId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<OrderTask>>(ok.Value);
        var task = Assert.Single(payload);
        Assert.Equal(9, task.Id);
        Assert.Equal("Confirm shipment", task.Name);
        _taskService.Verify(s => s.GetTasksByOrderIdAsync(orderId), Times.Once);
    }
}
