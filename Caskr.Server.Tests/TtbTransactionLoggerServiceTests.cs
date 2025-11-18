using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Caskr.Server.Tests;

public class TtbTransactionLoggerServiceTests
{
    [Fact]
    public async Task LogProductionAsync_ShouldPersistTransactionWithCalculatedProofGallons()
    {
        var (service, context) = CreateService();
        SeedCompanyHierarchy(context);

        await service.LogProductionAsync(1, new DateTime(2025, 02, 01));

        var transaction = await context.TtbTransactions.SingleAsync();
        Assert.Equal(TtbTransactionType.Production, transaction.TransactionType);
        Assert.Equal("Bourbon", transaction.ProductType);
        Assert.Equal(TtbSpiritsType.Under190Proof, transaction.SpiritsType);
        Assert.Equal(1, transaction.CompanyId);
        Assert.Equal(nameof(Batch), transaction.SourceEntityType);
        Assert.Equal(1, transaction.SourceEntityId);

        var expectedWineGallons = 10 * 53m; // 10 barrels × 53 gallons
        var expectedProof = Math.Round(expectedWineGallons * (62.5m / 100m), 2);
        Assert.Equal(expectedWineGallons, transaction.WineGallons);
        Assert.Equal(expectedProof, transaction.ProofGallons);
    }

    [Fact]
    public async Task LogLossAsync_ShouldUseProvidedProofGallonsAndReason()
    {
        var (service, context) = CreateService();
        SeedCompanyHierarchy(context);

        await service.LogLossAsync(1, 12.5m, "Evaporation");

        var transaction = await context.TtbTransactions.SingleAsync();
        Assert.Equal(TtbTransactionType.Loss, transaction.TransactionType);
        Assert.Equal(12.5m, transaction.ProofGallons);
        Assert.Contains("Evaporation", transaction.Notes);
        Assert.Equal(nameof(Barrel), transaction.SourceEntityType);
        Assert.Equal(1, transaction.SourceEntityId);

        var expectedWine = Math.Round(12.5m / (62.5m / 100m), 2);
        Assert.Equal(expectedWine, transaction.WineGallons);
    }

    private static (ITtbTransactionLogger Service, CaskrDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CaskrDbContext(options);
        var service = new TtbTransactionLoggerService(context, NullLogger<TtbTransactionLoggerService>.Instance);
        return (service, context);
    }

    private static void SeedCompanyHierarchy(CaskrDbContext context)
    {
        var company = new Company
        {
            Id = 1,
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        var userType = new UserType { Id = 1, Name = "Admin" };
        var status = new Status { Id = 1, Name = "In Progress" };
        var spiritType = new SpiritType { Id = 1, Name = "Bourbon" };
        var mashBill = new MashBill { Id = 1, CompanyId = 1, Name = "Standard" };
        var batch = new Batch { Id = 1, CompanyId = 1, MashBillId = 1, MashBill = mashBill };

        var user = new User
        {
            Id = 1,
            Name = "Owner",
            Email = "owner@example.invalid",
            CompanyId = 1,
            UserTypeId = 1,
            IsPrimaryContact = true,
            Company = company,
            UserType = userType
        };

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
            Quantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rickhouse = new Rickhouse
        {
            Id = 1,
            CompanyId = 1,
            Name = "Main Warehouse",
            Address = "123 Rickhouse Rd",
            Company = company
        };

        var barrel = new Barrel
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
        context.Barrels.Add(barrel);
        context.SaveChanges();
    }
}
