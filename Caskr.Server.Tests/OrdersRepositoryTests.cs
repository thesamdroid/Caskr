using Caskr.server.Models;
using Caskr.server.Repos;
using Microsoft.EntityFrameworkCore;
using UserTypeEnum = Caskr.server.UserType;
using System.Linq;

namespace Caskr.Server.Tests;

public class OrdersRepositoryTests
{
    [Fact]
    public async Task AddOrderAsync_AssignsBatchIdPerCompany()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("AddOrderAsync_AssignsBatchIdPerCompany")
            .Options;

        using var context = new CaskrDbContext(options);

        context.Users.AddRange(
            new User { Id = 1, Name = "Owner1", Email = "owner1@example.com", UserTypeId = (int)UserTypeEnum.Distiller, CompanyId = 1 },
            new User { Id = 2, Name = "Owner2", Email = "owner2@example.com", UserTypeId = (int)UserTypeEnum.Distiller, CompanyId = 1 },
            new User { Id = 3, Name = "Owner3", Email = "owner3@example.com", UserTypeId = (int)UserTypeEnum.Distiller, CompanyId = 2 }
        );

        context.MashBills.Add(new MashBill { Id = 1, CompanyId = 1, Name = "MB1" });
        context.Batches.Add(new Batch { Id = 1, CompanyId = 1, MashBillId = 1 });

        context.Orders.Add(new Order
        {
            Id = 1,
            Name = "Existing",
            OwnerId = 1,
            CompanyId = 1,
            StatusId = 1,
            SpiritTypeId = 1,
            BatchId = 1,
            Quantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var repo = new OrdersRepository(context);

        var orderSameCompany = new Order { Name = "New1", OwnerId = 2, StatusId = 1, SpiritTypeId = 1, Quantity = 20, MashBillId = 1 };
        var added1 = await repo.AddOrderAsync(orderSameCompany);
        Assert.Equal(2, added1.BatchId);

        var orderDifferentCompany = new Order { Name = "New2", OwnerId = 3, StatusId = 1, SpiritTypeId = 1, Quantity = 30, MashBillId = 1 };
        var added2 = await repo.AddOrderAsync(orderDifferentCompany);
        Assert.Equal(1, added2.BatchId);
    }

    [Fact]
    public async Task GetOrdersForOwnerAsync_ReturnsOrdersWithDetails()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("GetOrdersForOwnerAsync_ReturnsOrdersWithDetails")
            .Options;

        using var context = new CaskrDbContext(options);

        context.Statuses.Add(new Status { Id = 1, Name = "Open" });
        context.SpiritTypes.Add(new SpiritType { Id = 1, Name = "Whiskey" });

        context.Orders.AddRange(
            new Order
            {
                Id = 1,
                Name = "Owner1Order",
                OwnerId = 1,
                CompanyId = 1,
                StatusId = 1,
                SpiritTypeId = 1,
                BatchId = 1,
                Quantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Order
            {
                Id = 2,
                Name = "OtherOwnerOrder",
                OwnerId = 2,
                CompanyId = 1,
                StatusId = 1,
                SpiritTypeId = 1,
                BatchId = 1,
                Quantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var repo = new OrdersRepository(context);

        var orders = await repo.GetOrdersForOwnerAsync(1);
        var order = Assert.Single(orders);
        Assert.Equal(1, order.Id);
        Assert.NotNull(order.Status);
        Assert.NotNull(order.SpiritType);
    }

    [Fact]
    public async Task GetOrdersForCompanyAsync_ReturnsOrdersWithDetails()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("GetOrdersForCompanyAsync_ReturnsOrdersWithDetails")
            .Options;

        using var context = new CaskrDbContext(options);

        context.Statuses.Add(new Status { Id = 1, Name = "Open" });
        context.SpiritTypes.Add(new SpiritType { Id = 1, Name = "Whiskey" });

        context.Orders.AddRange(
            new Order
            {
                Id = 1,
                Name = "Company1Order1",
                OwnerId = 1,
                CompanyId = 1,
                StatusId = 1,
                SpiritTypeId = 1,
                BatchId = 1,
                Quantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Order
            {
                Id = 2,
                Name = "Company1Order2",
                OwnerId = 2,
                CompanyId = 1,
                StatusId = 1,
                SpiritTypeId = 1,
                BatchId = 1,
                Quantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Order
            {
                Id = 3,
                Name = "Company2Order",
                OwnerId = 3,
                CompanyId = 2,
                StatusId = 1,
                SpiritTypeId = 1,
                BatchId = 1,
                Quantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var repo = new OrdersRepository(context);

        var orders = await repo.GetOrdersForCompanyAsync(1);
        Assert.Equal(2, orders.Count());
        Assert.All(orders, o =>
        {
            Assert.Equal(1, o.CompanyId);
            Assert.NotNull(o.Status);
            Assert.NotNull(o.SpiritType);
        });
    }

    [Fact]
    public async Task AddOrderAsync_NullOrder_Throws()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("AddOrderAsync_NullOrder_Throws")
            .Options;

        using var context = new CaskrDbContext(options);
        var repo = new OrdersRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddOrderAsync(null));
    }

    [Fact]
    public async Task AddOrderAsync_OwnerNotFound_Throws()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("AddOrderAsync_OwnerNotFound_Throws")
            .Options;

        using var context = new CaskrDbContext(options);
        var repo = new OrdersRepository(context);
        var order = new Order { OwnerId = 99, StatusId = 1, SpiritTypeId = 1, MashBillId = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddOrderAsync(order));
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsNull_WhenMissing()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("GetOrderAsync_ReturnsNull_WhenMissing")
            .Options;

        using var context = new CaskrDbContext(options);
        var repo = new OrdersRepository(context);

        var result = await repo.GetOrderAsync(123);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddTasksForStatusAsync_NoNewTasks_NoChanges()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("AddTasksForStatusAsync_NoNewTasks_NoChanges")
            .Options;

        using var context = new CaskrDbContext(options);
        context.StatusTasks.Add(new StatusTask { Id = 1, StatusId = 1, Name = "Task1" });
        context.Orders.Add(new Order
        {
            Id = 1,
            Name = "Order",
            OwnerId = 1,
            CompanyId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StatusId = 1,
            SpiritTypeId = 1,
            BatchId = 1,
            Quantity = 1
        });
        await context.SaveChangesAsync();

        var repo = new OrdersRepository(context);
        await repo.AddTasksForStatusAsync(1, 1);
        Assert.Equal(1, context.Tasks.Count());

        await repo.AddTasksForStatusAsync(1, 1);
        Assert.Equal(1, context.Tasks.Count());
    }
}

