using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Caskr.Server.Tests;

public class TtbInventorySnapshotCalculatorTests
{
    [Fact]
    public async Task BuildSnapshotRowsAsync_ShouldAggregateActiveBarrels()
    {
        await using var context = CreateContext();
        SeedBarrels(
            context,
            "Aging",
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        var calculator = new TtbInventorySnapshotCalculator(context, NullLogger<TtbInventorySnapshotCalculator>.Instance);
        var results = await calculator.BuildSnapshotRowsAsync(1, new DateTime(2025, 02, 15), CancellationToken.None);

        var snapshot = Assert.Single(results);
        Assert.Equal("Bourbon", snapshot.ProductType);
        Assert.Equal(TtbSpiritsType.Under190Proof, snapshot.SpiritsType);
        Assert.Equal(TtbTaxStatus.Bonded, snapshot.TaxStatus);
        Assert.Equal(53m * 2, snapshot.WineGallons);

        var expectedProof = Math.Round((53m * 2) * ((62.5m * 2m) / 100m), 2);
        Assert.Equal(expectedProof, snapshot.ProofGallons);
    }

    [Fact]
    public async Task BuildSnapshotRowsAsync_ShouldIgnoreInactiveStatuses()
    {
        await using var context = CreateContext();
        SeedBarrels(
            context,
            "Sold",
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 02, 10, 0, 0, 0, DateTimeKind.Utc));

        var calculator = new TtbInventorySnapshotCalculator(context, NullLogger<TtbInventorySnapshotCalculator>.Instance);
        var results = await calculator.BuildSnapshotRowsAsync(1, new DateTime(2025, 02, 15), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task BuildSnapshotRowsAsync_ShouldRespectSnapshotDateWhenStatusChanges()
    {
        await using var context = CreateContext();
        SeedBarrels(
            context,
            "Sold",
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 02, 25, 0, 0, 0, DateTimeKind.Utc));

        var calculator = new TtbInventorySnapshotCalculator(context, NullLogger<TtbInventorySnapshotCalculator>.Instance);
        var historicalResults = await calculator.BuildSnapshotRowsAsync(1, new DateTime(2025, 02, 15), CancellationToken.None);
        var historicalSnapshot = Assert.Single(historicalResults);
        Assert.Equal(53m * 2, historicalSnapshot.WineGallons);

        var postSaleResults = await calculator.BuildSnapshotRowsAsync(1, new DateTime(2025, 03, 01), CancellationToken.None);
        Assert.Empty(postSaleResults);
    }

    private static CaskrDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CaskrDbContext(options);
    }

    private static void SeedBarrels(
        CaskrDbContext context,
        string statusName,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        var updated = updatedAt ?? created;
        var company = new Company
        {
            Id = 1,
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        var userType = new UserType { Id = 1, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Name = "Owner",
            Email = "owner@example.invalid",
            CompanyId = 1,
            Company = company,
            UserTypeId = 1,
            UserType = userType,
            IsPrimaryContact = true
        };

        var status = new Status { Id = 1, Name = statusName };
        var spiritType = new SpiritType { Id = 1, Name = "Bourbon" };
        var mashBill = new MashBill { Id = 1, CompanyId = 1, Name = "Standard" };
        var batch = new Batch { Id = 1, CompanyId = 1, MashBillId = 1, MashBill = mashBill };

        var order = new Order
        {
            Id = 1,
            Name = "Order 1",
            OwnerId = 1,
            Owner = user,
            CompanyId = 1,
            StatusId = 1,
            Status = status,
            SpiritTypeId = 1,
            SpiritType = spiritType,
            BatchId = 1,
            Batch = batch,
            Quantity = 2,
            CreatedAt = created,
            UpdatedAt = updated
        };

        var rickhouse = new Rickhouse
        {
            Id = 1,
            CompanyId = 1,
            Name = "Main Warehouse",
            Address = "123 Rickhouse Rd",
            Company = company
        };

        var barrels = new List<Barrel>
        {
            new()
            {
                Id = 1,
                CompanyId = 1,
                Sku = "BAR-001",
                BatchId = 1,
                Batch = batch,
                OrderId = 1,
                Order = order,
                RickhouseId = 1,
                Rickhouse = rickhouse
            },
            new()
            {
                Id = 2,
                CompanyId = 1,
                Sku = "BAR-002",
                BatchId = 1,
                Batch = batch,
                OrderId = 1,
                Order = order,
                RickhouseId = 1,
                Rickhouse = rickhouse
            }
        };

        context.Companies.Add(company);
        context.UserTypes.Add(userType);
        context.Users.Add(user);
        context.Statuses.Add(status);
        context.SpiritTypes.Add(spiritType);
        context.MashBills.Add(mashBill);
        context.Batches.Add(batch);
        context.Orders.Add(order);
        context.Rickhouses.Add(rickhouse);
        context.Barrels.AddRange(barrels);
        context.SaveChanges();
    }
}
