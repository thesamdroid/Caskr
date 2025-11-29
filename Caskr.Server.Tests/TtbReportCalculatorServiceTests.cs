using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Tests;

public class TtbReportCalculatorServiceTests
{
    [Fact]
    public async Task CalculateMonthlyReportAsync_UsesSnapshotAndCalculatesSections()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 1;
        var month = 10;
        var year = 2024;

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 9, 1),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100m,
            WineGallons = 50m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 10, 31),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 120m,
            WineGallons = 60m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        context.TtbTransactions.AddRange(
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 5),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 20m,
                WineGallons = 10m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 7),
                TransactionType = TtbTransactionType.TransferIn,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 5m,
                WineGallons = 2.5m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 15),
                TransactionType = TtbTransactionType.TransferOut,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 3m,
                WineGallons = 1.5m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 20),
                TransactionType = TtbTransactionType.Loss,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 2m,
                WineGallons = 1m
            });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, month, year);

        Assert.Equal(new DateTime(2024, 10, 1), result.StartDate);
        Assert.Equal(new DateTime(2024, 10, 31), result.EndDate);

        var opening = Assert.Single(result.OpeningInventory.Rows);
        Assert.Equal(100m, opening.ProofGallons);
        Assert.Equal(50m, opening.WineGallons);

        var production = Assert.Single(result.Production.Rows);
        Assert.Equal(20m, production.ProofGallons);
        Assert.Equal(10m, production.WineGallons);

        var transfersIn = Assert.Single(result.Transfers.TransfersIn);
        Assert.Equal(5m, transfersIn.ProofGallons);
        Assert.Equal(2.5m, transfersIn.WineGallons);

        var transfersOut = Assert.Single(result.Transfers.TransfersOut);
        Assert.Equal(3m, transfersOut.ProofGallons);
        Assert.Equal(1.5m, transfersOut.WineGallons);

        var losses = Assert.Single(result.Losses.Rows);
        Assert.Equal(2m, losses.ProofGallons);
        Assert.Equal(1m, losses.WineGallons);

        var closing = Assert.Single(result.ClosingInventory.Rows);
        Assert.Equal(120m, closing.ProofGallons);
        Assert.Equal(60m, closing.WineGallons);
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_UsesMostRecentSnapshotBeforeStart()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 3;
        var month = 10;
        var year = 2024;

        context.TtbInventorySnapshots.AddRange(
            new TtbInventorySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = new DateTime(2024, 9, 1),
                ProductType = "Vodka",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 50m,
                WineGallons = 25m,
                TaxStatus = TtbTaxStatus.Bonded
            },
            new TtbInventorySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = new DateTime(2024, 9, 30),
                ProductType = "Vodka",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 75m,
                WineGallons = 37.5m,
                TaxStatus = TtbTaxStatus.Bonded
            });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, month, year);

        var opening = Assert.Single(result.OpeningInventory.Rows);
        Assert.Equal(75m, opening.ProofGallons);
        Assert.Equal(37.5m, opening.WineGallons);
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_FallsBackToTransactionsWhenSnapshotMissing()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 2;
        var month = 8;
        var year = 2024;
        var startDate = new DateTime(year, month, 1);

        context.TtbTransactions.AddRange(
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = startDate.AddDays(-5),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Rum",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 40m,
                WineGallons = 20m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = startDate.AddDays(-2),
                TransactionType = TtbTransactionType.TransferOut,
                ProductType = "Rum",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 10m,
                WineGallons = 5m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = startDate.AddDays(5),
                TransactionType = TtbTransactionType.Loss,
                ProductType = "Rum",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 4m,
                WineGallons = 2m
            });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, month, year);

        var opening = Assert.Single(result.OpeningInventory.Rows);
        Assert.Equal(30m, opening.ProofGallons);
        Assert.Equal(15m, opening.WineGallons);

        var losses = Assert.Single(result.Losses.Rows);
        Assert.Equal(4m, losses.ProofGallons);
        Assert.Equal(2m, losses.WineGallons);

        var closing = Assert.Single(result.ClosingInventory.Rows);
        Assert.Equal(26m, closing.ProofGallons);
        Assert.Equal(13m, closing.WineGallons);
    }

    [Fact]
    public async Task CalculateForm5110_40Async_ComputesStorageBalances()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 5;
        var month = 6;
        var year = 2024;

        context.Rickhouses.Add(new Rickhouse
        {
            Id = 1,
            CompanyId = companyId,
            Name = "Rickhouse A",
            Address = "123 Rick St"
        });

        context.TtbInventorySnapshots.AddRange(
            new TtbInventorySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = new DateTime(2024, 5, 31),
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 120m,
                WineGallons = 106m,
                TaxStatus = TtbTaxStatus.Bonded
            },
            new TtbInventorySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = new DateTime(2024, 6, 30),
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 180m,
                WineGallons = 159m,
                TaxStatus = TtbTaxStatus.Bonded
            });

        context.TtbTransactions.Add(new TtbTransaction
        {
            CompanyId = companyId,
            TransactionDate = new DateTime(2024, 6, 10),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 60m,
            WineGallons = 53m
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateForm5110_40Async(companyId, month, year);

        Assert.Equal(2m, result.OpeningBarrels);
        Assert.Equal(1m, result.BarrelsReceived);
        Assert.Equal(0m, result.BarrelsRemoved);
        Assert.Equal(3m, result.ClosingBarrels);

        var warehouseTotals = Assert.Single(result.ProofGallonsByWarehouse);
        Assert.Equal("Rickhouse A", warehouseTotals.WarehouseName);
        Assert.Equal(180m, warehouseTotals.ProofGallons);
    }

    [Fact]
    public async Task CalculateForm5110_40Async_WhenClosingSnapshotMismatches_Throws()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 8;

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 4, 30),
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 106m,
            WineGallons = 53m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 5, 31),
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 200m,
            WineGallons = 100m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateForm5110_40Async(companyId, 5, 2024));
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_ValidationPasses_WhenDataIsCorrect()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 100;
        var month = 10;
        var year = 2024;

        // Add company with TTB permit
        context.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = "Test Distillery",
            TtbPermitNumber = "DSP-KY-12345"
        });

        // Add opening snapshot
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 9, 30),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100m,
            WineGallons = 50m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Add closing snapshot
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 10, 31),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 115m,
            WineGallons = 57.5m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Add transactions that balance correctly
        context.TtbTransactions.AddRange(
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 5),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 20m,
                WineGallons = 10m
            },
            new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = new DateTime(2024, 10, 20),
                TransactionType = TtbTransactionType.Loss,
                ProductType = "Whiskey",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 5m,
                WineGallons = 2.5m
            });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, month, year);

        Assert.True(result.Validation.IsValid);
        Assert.Empty(result.Validation.Errors);
        Assert.Empty(result.Validation.Warnings);
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_ValidationFails_WhenTtbPermitMissing()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 101;

        // Add company without TTB permit
        context.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = "Test Distillery",
            TtbPermitNumber = null
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, 10, 2024);

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("TTB permit number is missing"));
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_ValidationWarns_WhenLossPercentageHigh()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 102;

        context.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = "Test Distillery",
            TtbPermitNumber = "DSP-KY-12345"
        });

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 9, 30),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100m,
            WineGallons = 50m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 10, 31),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 84m,
            WineGallons = 42m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Add high loss transaction (16% of opening)
        context.TtbTransactions.Add(new TtbTransaction
        {
            CompanyId = companyId,
            TransactionDate = new DateTime(2024, 10, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 16m,
            WineGallons = 8m
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, 10, 2024);

        Assert.Contains(result.Validation.Warnings, w => w.Contains("Loss percentage") && w.Contains("unusually high"));
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_ValidationFails_WhenInventoryReconciliationFails()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 103;

        context.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = "Test Distillery",
            TtbPermitNumber = "DSP-KY-12345"
        });

        // Opening snapshot
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 9, 30),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100m,
            WineGallons = 50m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Closing snapshot that doesn't balance with transactions
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 10, 31),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 150m, // Should be 120 based on transactions
            WineGallons = 75m,   // Should be 60 based on transactions
            TaxStatus = TtbTaxStatus.Bonded
        });

        context.TtbTransactions.Add(new TtbTransaction
        {
            CompanyId = companyId,
            TransactionDate = new DateTime(2024, 10, 5),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 20m,
            WineGallons = 10m
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, 10, 2024);

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Inventory reconciliation failed"));
    }

    [Fact]
    public async Task CalculateMonthlyReportAsync_ValidationFails_WhenNegativeInventory()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);
        var companyId = 104;

        context.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = "Test Distillery",
            TtbPermitNumber = "DSP-KY-12345"
        });

        // Very small opening inventory
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 9, 30),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 5m,
            WineGallons = 2.5m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Closing would be negative
        context.TtbInventorySnapshots.Add(new TtbInventorySnapshot
        {
            CompanyId = companyId,
            SnapshotDate = new DateTime(2024, 10, 31),
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = -5m,
            WineGallons = -2.5m,
            TaxStatus = TtbTaxStatus.Bonded
        });

        // Large transfer out that exceeds opening
        context.TtbTransactions.Add(new TtbTransaction
        {
            CompanyId = companyId,
            TransactionDate = new DateTime(2024, 10, 10),
            TransactionType = TtbTransactionType.TransferOut,
            ProductType = "Whiskey",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10m,
            WineGallons = 5m
        });

        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<TtbReportCalculatorService>();
        var service = new TtbReportCalculatorService(context, logger);

        var result = await service.CalculateMonthlyReportAsync(companyId, 10, 2024);

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Negative inventory detected"));
    }
}
