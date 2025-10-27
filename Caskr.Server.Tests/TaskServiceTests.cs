using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Caskr.Server.Tests;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateTaskAsync_TrimsNameAndPersistsAssignee()
    {
        await using var context = CreateContext();
        var service = new TaskService(context, NullLogger<TaskService>.Instance);

        var dueDate = DateTime.UtcNow.AddDays(1);
        var task = await service.CreateTaskAsync(1, "  Bottling Prep  ", 2, dueDate);

        Assert.Equal("Bottling Prep", task.Name);
        Assert.Equal(1, task.OrderId);
        Assert.Equal(2, task.AssigneeId);
        Assert.Equal(dueDate, task.DueDate);
        Assert.False(task.IsComplete);

        var persisted = await context.OrderTasks.AsNoTracking().SingleAsync();
        Assert.Equal(task.Id, persisted.Id);
        Assert.Equal("Bottling Prep", persisted.Name);
        Assert.Equal(2, persisted.AssigneeId);
    }

    [Fact]
    public async Task AssignTaskAsync_AllowsUnassigningAndUpdatesTimestamp()
    {
        await using var context = CreateContext();
        var service = new TaskService(context, NullLogger<TaskService>.Instance);

        var task = await service.CreateTaskAsync(1, "Review Labels", 2, null);
        var initialUpdatedAt = task.UpdatedAt;

        var updated = await service.AssignTaskAsync(task.Id, null);

        Assert.Null(updated.AssigneeId);
        Assert.True(updated.UpdatedAt > initialUpdatedAt);
        Assert.Null(updated.Assignee);
    }

    private static CaskrDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CaskrDbContext(options);

        var company = new Company
        {
            Id = 1,
            CompanyName = "Test Co",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        var userType = new UserType { Id = 1, Name = "Admin" };
        var owner = new User
        {
            Id = 1,
            Name = "Owner",
            Email = "owner@example.com",
            CompanyId = company.Id,
            Company = company,
            UserTypeId = userType.Id,
            UserType = userType,
            IsPrimaryContact = true
        };

        var assignee = new User
        {
            Id = 2,
            Name = "Assignee",
            Email = "assignee@example.com",
            CompanyId = company.Id,
            Company = company,
            UserTypeId = userType.Id,
            UserType = userType,
            IsPrimaryContact = false
        };

        var status = new Status { Id = 1, Name = "Open" };
        var spiritType = new SpiritType { Id = 1, Name = "Bourbon" };
        var mashBill = new MashBill { Id = 1, CompanyId = company.Id, Name = "Mash" };
        var batch = new Batch { Id = 1, CompanyId = company.Id, MashBillId = mashBill.Id, MashBill = mashBill };

        var order = new Order
        {
            Id = 1,
            Name = "Test Order",
            OwnerId = owner.Id,
            Owner = owner,
            CompanyId = company.Id,
            StatusId = status.Id,
            Status = status,
            SpiritTypeId = spiritType.Id,
            SpiritType = spiritType,
            BatchId = batch.Id,
            Batch = batch,
            Quantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Companies.Add(company);
        context.UserTypes.Add(userType);
        context.Users.AddRange(owner, assignee);
        context.Statuses.Add(status);
        context.SpiritTypes.Add(spiritType);
        context.MashBills.Add(mashBill);
        context.Batches.Add(batch);
        context.Orders.Add(order);
        context.SaveChanges();

        return context;
    }
}
