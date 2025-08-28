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
}
