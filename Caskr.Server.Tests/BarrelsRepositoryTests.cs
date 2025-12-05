using System;
using Caskr.server.Models;
using Caskr.server.Repos;
using Microsoft.EntityFrameworkCore;

namespace Caskr.Server.Tests;

public class BarrelsRepositoryTests
{
    [Fact]
    public async Task ForecastBarrelsAsync_FiltersByAge()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase("ForecastBarrelsAsync_FiltersByAge")
            .Options;

        using var context = new CaskrDbContext(options);

        // minimal related data
        context.Statuses.Add(new Status { Id = 1, Name = "Open" });
        context.SpiritTypes.Add(new SpiritType { Id = 1, Name = "Whiskey" });
        context.MashBills.Add(new MashBill { Id = 1, CompanyId = 1, Name = "MB" });
        context.Batches.Add(new Batch { Id = 1, CompanyId = 1, MashBillId = 1 });
        context.Rickhouses.Add(new Rickhouse { Id = 1, CompanyId = 1, Name = "R1", Address = "A" });

        context.Orders.AddRange(
            new Order
            {
                Id = 1,
                Name = "Old", OwnerId = 1, CompanyId = 1, StatusId = 1, SpiritTypeId = 1,
                BatchId = 1, Quantity = 1,
                CreatedAt = DateTime.UtcNow.AddYears(-6),
                UpdatedAt = DateTime.UtcNow.AddYears(-6)
            },
            new Order
            {
                Id = 2,
                Name = "Young", OwnerId = 1, CompanyId = 1, StatusId = 1, SpiritTypeId = 1,
                BatchId = 1, Quantity = 1,
                CreatedAt = DateTime.UtcNow.AddYears(-3),
                UpdatedAt = DateTime.UtcNow.AddYears(-3)
            }
        );

        context.Barrels.AddRange(
            new Barrel { Id = 1, CompanyId = 1, Sku = "A", BatchId = 1, OrderId = 1, RickhouseId = 1 },
            new Barrel { Id = 2, CompanyId = 1, Sku = "B", BatchId = 1, OrderId = 2, RickhouseId = 1 }
        );

        await context.SaveChangesAsync();

        var repo = new BarrelsRepository(context);
        var result = await repo.ForecastBarrelsAsync(1, DateTime.UtcNow, 5);

        var barrels = result.ToList();
        var single = Assert.Single(barrels);
        Assert.Equal(1, single.Id);
    }

    [Fact]
    public async Task CreateBatchAsync_AssignsNextIdPerCompany()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        context.MashBills.Add(new MashBill { Id = 1, CompanyId = 1, Name = "MB" });
        context.Batches.Add(new Batch { Id = 1, CompanyId = 1, MashBillId = 1 });
        await context.SaveChangesAsync();

        var repo = new BarrelsRepository(context);
        var newBatchId = await repo.CreateBatchAsync(1, 1);

        Assert.Equal(2, newBatchId);
        var batch = await context.Batches.SingleAsync(b => b.CompanyId == 1 && b.Id == newBatchId);
        Assert.Equal(1, batch.MashBillId);
    }

    [Fact]
    public async Task EnsureOrderForBatchAsync_CreatesAndUpdatesOrder()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        context.Statuses.Add(new Status { Id = 1, Name = "Open" });
        context.SpiritTypes.Add(new SpiritType { Id = 1, Name = "Whiskey" });
        context.Batches.Add(new Batch { Id = 1, CompanyId = 1, MashBillId = 1 });
        await context.SaveChangesAsync();

        var repo = new BarrelsRepository(context);
        var orderId = await repo.EnsureOrderForBatchAsync(1, 10, 1, 5);
        var createdOrder = await context.Orders.FindAsync(orderId);
        Assert.NotNull(createdOrder);
        Assert.Equal(5, createdOrder!.Quantity);

        var secondCallOrderId = await repo.EnsureOrderForBatchAsync(1, 10, 1, 2);
        Assert.Equal(orderId, secondCallOrderId);
        var updatedOrder = await context.Orders.FindAsync(orderId);
        Assert.Equal(7, updatedOrder!.Quantity);
    }

    [Fact]
    public async Task GetRickhouseIdsByNameAsync_ReturnsMatchesIgnoringCase()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        context.Rickhouses.AddRange(
            new Rickhouse { Id = 1, CompanyId = 1, Name = "Alpha", Address = "A" },
            new Rickhouse { Id = 2, CompanyId = 1, Name = "Beta", Address = "B" },
            new Rickhouse { Id = 3, CompanyId = 2, Name = "Gamma", Address = "C" }
        );
        await context.SaveChangesAsync();

        var repo = new BarrelsRepository(context);
        var map = await repo.GetRickhouseIdsByNameAsync(1, new[] { "alpha", "beta", "missing" });

        Assert.Equal(2, map!.Count);
        Assert.Equal(1, map["alpha"]);
        Assert.Equal(2, map["beta"]);
    }
}
