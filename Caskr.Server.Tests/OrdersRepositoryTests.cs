using Caskr.server.Models;
using Caskr.server.Repos;
using Microsoft.EntityFrameworkCore;

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
            new User { Id = 1, Name = "Owner1", Email = "owner1@example.com", UserTypeId = 1, CompanyId = 1 },
            new User { Id = 2, Name = "Owner2", Email = "owner2@example.com", UserTypeId = 1, CompanyId = 1 },
            new User { Id = 3, Name = "Owner3", Email = "owner3@example.com", UserTypeId = 1, CompanyId = 2 }
        );

        context.Orders.Add(new Order
        {
            Id = 1,
            Name = "Existing",
            OwnerId = 1,
            StatusId = 1,
            SpiritTypeId = 1,
            BatchId = 1,
            Quantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var repo = new OrdersRepository(context);

        var orderSameCompany = new Order { Name = "New1", OwnerId = 2, StatusId = 1, SpiritTypeId = 1, Quantity = 20 };
        var added1 = await repo.AddOrderAsync(orderSameCompany);
        Assert.Equal(2, added1.BatchId);

        var orderDifferentCompany = new Order { Name = "New2", OwnerId = 3, StatusId = 1, SpiritTypeId = 1, Quantity = 30 };
        var added2 = await repo.AddOrderAsync(orderDifferentCompany);
        Assert.Equal(1, added2.BatchId);
    }
}

