using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ITtbReportCalculator
{
    Task<TtbMonthlyReportData> CalculateMonthlyReportAsync(int companyId, int month, int year, CancellationToken cancellationToken = default);

    Task<TtbForm5110_40Data> CalculateForm5110_40Async(
        int companyId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Calculates TTB Form 5110.28 (Monthly Report of Processing Operations) and
/// Form 5110.40 (Monthly Report of Storage Operations) data.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md
/// REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V - Records and Reports
///
/// This service implements the official TTB inventory balance equation:
///   Closing Inventory = Opening Inventory
///                      + Production
///                      + Transfers In
///                      - Transfers Out
///                      - Losses
///
/// All calculations must balance within 0.01 proof gallons (federal regulation).
/// Transaction multipliers are fixed by TTB regulation and cannot be modified.
///
/// CRITICAL: This service generates data for federal compliance reporting.
/// Any modification must be reviewed against TTB regulations and the mapping document.
/// Incorrect calculations can result in federal penalties and license suspension.
/// </summary>
public sealed class TtbReportCalculatorService(
    CaskrDbContext dbContext,
    ILogger<TtbReportCalculatorService> logger) : ITtbReportCalculator
{
    // TTB regulation allows 0.01 proof gallon tolerance for rounding
    // See: docs/TTB_FORM_5110_28_MAPPING.md, Section "Calculation Formulas"
    private const decimal SnapshotTolerance = 0.01m;

    // TTB regulation standard barrel size
    // See docs/TTB_COMPLIANCE_GUIDE.md, Section "Critical Compliance Rules"
    private const decimal StandardBarrelWineGallons = 53m;

    public async Task<TtbMonthlyReportData> CalculateMonthlyReportAsync(int companyId, int month, int year, CancellationToken cancellationToken = default)
    {
        ValidateMonthAndYear(month, year);

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var openingInventory = await LoadOpeningInventoryAsync(companyId, startDate, cancellationToken);
        var monthlyTransactions = await dbContext.TtbTransactions
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .ToListAsync(cancellationToken);

        var production = AggregateTransactions(monthlyTransactions, TtbTransactionType.Production, applyDirectionalMultipliers: false);
        var transfersIn = AggregateTransactions(monthlyTransactions, TtbTransactionType.TransferIn, applyDirectionalMultipliers: false);
        var transfersOut = AggregateTransactions(monthlyTransactions, TtbTransactionType.TransferOut, applyDirectionalMultipliers: false);
        var losses = AggregateTransactions(monthlyTransactions, TtbTransactionType.Loss, applyDirectionalMultipliers: false);

        var closingInventory = CalculateClosingInventory(openingInventory, production, transfersIn, transfersOut, losses);
        await ValidateAgainstClosingSnapshotAsync(companyId, endDate, closingInventory, cancellationToken);

        return new TtbMonthlyReportData
        {
            CompanyId = companyId,
            Month = month,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            OpeningInventory = new InventorySection { Rows = ToTotals(openingInventory) },
            Production = new ProductionSection { Rows = ToTotals(production) },
            Transfers = new TransfersSection
            {
                TransfersIn = ToTotals(transfersIn),
                TransfersOut = ToTotals(transfersOut)
            },
            Losses = new LossSection { Rows = ToTotals(losses) },
            ClosingInventory = new InventorySection { Rows = ToTotals(closingInventory) }
        };
    }

    public async Task<TtbForm5110_40Data> CalculateForm5110_40Async(
        int companyId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidateMonthAndYear(month, year);

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var openingInventory = await LoadOpeningInventoryAsync(companyId, startDate, cancellationToken);
        var openingTotals = SumAggregate(openingInventory);

        var monthlyTransactions = await dbContext.TtbTransactions
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .ToListAsync(cancellationToken);

        var receivedTransactions = monthlyTransactions
            .Where(t => t.TransactionType is TtbTransactionType.Production or TtbTransactionType.TransferIn)
            .ToList();
        var removedTransactions = monthlyTransactions
            .Where(t => t.TransactionType is TtbTransactionType.TransferOut or TtbTransactionType.Bottling)
            .ToList();

        var receivedAggregate = AggregateTransactions(receivedTransactions, applyDirectionalMultipliers: false);
        var removedAggregate = AggregateTransactions(removedTransactions, applyDirectionalMultipliers: false);

        var calculatedClosing = CalculateClosingInventory(
            openingInventory,
            receivedAggregate,
            new Dictionary<SectionKey, SectionAggregate>(new SectionKeyComparer()),
            removedAggregate,
            new Dictionary<SectionKey, SectionAggregate>(new SectionKeyComparer()));

        var closingSnapshots = await dbContext.TtbInventorySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.CompanyId == companyId && snapshot.SnapshotDate == endDate)
            .ToListAsync(cancellationToken);

        var closingInventory = closingSnapshots.Count > 0
            ? AggregateSnapshots(closingSnapshots)
            : calculatedClosing;

        await ValidateAgainstClosingSnapshotAsync(companyId, endDate, calculatedClosing, cancellationToken);

        var closingTotals = SumAggregate(closingInventory);
        var expectedClosingWineGallons = openingTotals.WineGallons
                                         + SumAggregate(receivedAggregate).WineGallons
                                         - SumAggregate(removedAggregate).WineGallons;

        if (Math.Abs(closingTotals.WineGallons - expectedClosingWineGallons) > SnapshotTolerance)
        {
            logger.LogError(
                "TTB COMPLIANCE ERROR: Storage inventory balance mismatch for {CompanyId} {Month}/{Year}. " +
                "Calculated closing = {Calculated}, Expected = {Expected}. See docs/TTB_FORM_5110_28_MAPPING.md Calculation Formulas section.",
                companyId,
                month,
                year,
                closingTotals.WineGallons,
                expectedClosingWineGallons);
            throw new InvalidOperationException("Inventory balance validation failed for storage report.");
        }

        if (closingTotals.WineGallons < 0 || closingTotals.ProofGallons < 0)
        {
            throw new InvalidOperationException("Negative inventory is not permitted for TTB storage reports.");
        }

        var openingBarrels = CalculateBarrelsFromWineGallons(openingTotals.WineGallons);
        var receivedBarrels = CalculateBarrelsFromWineGallons(SumAggregate(receivedAggregate).WineGallons);
        var removedBarrels = CalculateBarrelsFromWineGallons(SumAggregate(removedAggregate).WineGallons);
        var closingBarrels = CalculateBarrelsFromWineGallons(closingTotals.WineGallons);

        var warehouseProofGallons = await BuildWarehouseProofGallonsAsync(
            companyId,
            closingInventory,
            monthlyTransactions,
            cancellationToken);

        return new TtbForm5110_40Data
        {
            CompanyId = companyId,
            Month = month,
            Year = year,
            OpeningBarrels = openingBarrels,
            BarrelsReceived = receivedBarrels,
            BarrelsRemoved = removedBarrels,
            ClosingBarrels = closingBarrels,
            ProofGallonsByWarehouse = warehouseProofGallons
        };
    }

    private static void ValidateMonthAndYear(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        if (year < 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be 2000 or later.");
        }
    }

    private async Task<Dictionary<SectionKey, SectionAggregate>> LoadOpeningInventoryAsync(
        int companyId,
        DateTime startDate,
        CancellationToken cancellationToken)
    {
        var latestSnapshotDate = await dbContext.TtbInventorySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.CompanyId == companyId && snapshot.SnapshotDate < startDate)
            .MaxAsync(snapshot => (DateTime?)snapshot.SnapshotDate, cancellationToken);

        if (latestSnapshotDate.HasValue)
        {
            var snapshotRows = await dbContext.TtbInventorySnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.CompanyId == companyId && snapshot.SnapshotDate == latestSnapshotDate.Value)
                .ToListAsync(cancellationToken);

            if (snapshotRows.Count > 0)
            {
                return AggregateSnapshots(snapshotRows);
            }
        }

        var previousTransactions = await dbContext.TtbTransactions
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.TransactionDate < startDate)
            .ToListAsync(cancellationToken);

        if (previousTransactions.Count == 0)
        {
            logger.LogInformation(
                "No prior TTB inventory snapshots or transactions found for company {CompanyId} before {StartDate:yyyy-MM-dd}.",
                companyId,
                startDate);

            return new Dictionary<SectionKey, SectionAggregate>();
        }

        logger.LogWarning(
            "Opening inventory snapshot not found for company {CompanyId} before {StartDate:yyyy-MM-dd}. Falling back to aggregated prior transactions.",
            companyId,
            startDate);

        return AggregateTransactions(previousTransactions);
    }

    private async Task ValidateAgainstClosingSnapshotAsync(
        int companyId,
        DateTime endDate,
        IReadOnlyDictionary<SectionKey, SectionAggregate> calculatedClosing,
        CancellationToken cancellationToken)
    {
        var closingSnapshotDate = endDate.Date;
        var closingSnapshots = await dbContext.TtbInventorySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.CompanyId == companyId && snapshot.SnapshotDate == closingSnapshotDate)
            .ToListAsync(cancellationToken);

        if (closingSnapshots.Count == 0)
        {
            logger.LogWarning(
                "No closing inventory snapshot found for company {CompanyId} on {SnapshotDate:yyyy-MM-dd}. Calculated closing totals will be used without reconciliation.",
                companyId,
                closingSnapshotDate);
            return;
        }

        var snapshotAggregate = AggregateSnapshots(closingSnapshots);
        foreach (var key in snapshotAggregate.Keys.Union(calculatedClosing.Keys))
        {
            var calculated = calculatedClosing.TryGetValue(key, out var calculatedValue)
                ? calculatedValue
                : new SectionAggregate();

            var snapshot = snapshotAggregate.TryGetValue(key, out var snapshotValue)
                ? snapshotValue
                : new SectionAggregate();

            if (Math.Abs(calculated.ProofGallons - snapshot.ProofGallons) > SnapshotTolerance ||
                Math.Abs(calculated.WineGallons - snapshot.WineGallons) > SnapshotTolerance)
            {
                logger.LogWarning(
                    "Closing inventory mismatch for {ProductType}/{SpiritsType} on {SnapshotDate:yyyy-MM-dd}: calculated {CalculatedProof:F2} PG / {CalculatedWine:F2} WG vs snapshot {SnapshotProof:F2} PG / {SnapshotWine:F2} WG.",
                    key.ProductType,
                    key.SpiritsType,
                    closingSnapshotDate,
                    calculated.ProofGallons,
                    calculated.WineGallons,
                    snapshot.ProofGallons,
                    snapshot.WineGallons);
            }
        }
    }

    private static Dictionary<SectionKey, SectionAggregate> AggregateSnapshots(IEnumerable<TtbInventorySnapshot> snapshots)
    {
        var aggregate = new Dictionary<SectionKey, SectionAggregate>(new SectionKeyComparer());

        foreach (var snapshot in snapshots)
        {
            var key = new SectionKey(snapshot.ProductType, snapshot.SpiritsType);
            AddToAggregate(aggregate, key, snapshot.ProofGallons, snapshot.WineGallons);
        }

        return aggregate;
    }

    private static Dictionary<SectionKey, SectionAggregate> AggregateTransactions(
        IEnumerable<TtbTransaction> transactions,
        TtbTransactionType? filterType = null,
        bool applyDirectionalMultipliers = true)
    {
        var aggregate = new Dictionary<SectionKey, SectionAggregate>(new SectionKeyComparer());

        foreach (var transaction in transactions)
        {
            if (filterType.HasValue && transaction.TransactionType != filterType.Value)
            {
                continue;
            }

            var key = new SectionKey(transaction.ProductType, transaction.SpiritsType);
            var multiplier = applyDirectionalMultipliers
                ? GetTransactionMultiplier(transaction.TransactionType)
                : 1m;

            AddToAggregate(
                aggregate,
                key,
                transaction.ProofGallons * multiplier,
                transaction.WineGallons * multiplier);
        }

        return aggregate;
    }

    private static IReadOnlyCollection<TtbSectionTotal> ToTotals(IReadOnlyDictionary<SectionKey, SectionAggregate> aggregate)
    {
        if (aggregate.Count == 0)
        {
            return Array.Empty<TtbSectionTotal>();
        }

        var totals = new List<TtbSectionTotal>(aggregate.Count);

        foreach (var (key, value) in aggregate.OrderBy(entry => entry.Key.ProductType).ThenBy(entry => entry.Key.SpiritsType))
        {
            totals.Add(new TtbSectionTotal
            {
                ProductType = key.ProductType,
                SpiritsType = key.SpiritsType,
                WineGallons = Math.Round(value.WineGallons, 2, MidpointRounding.AwayFromZero),
                ProofGallons = Math.Round(value.ProofGallons, 2, MidpointRounding.AwayFromZero)
            });
        }

        return totals;
    }

    private static Dictionary<SectionKey, SectionAggregate> CalculateClosingInventory(
        IReadOnlyDictionary<SectionKey, SectionAggregate> opening,
        IReadOnlyDictionary<SectionKey, SectionAggregate> production,
        IReadOnlyDictionary<SectionKey, SectionAggregate> transfersIn,
        IReadOnlyDictionary<SectionKey, SectionAggregate> transfersOut,
        IReadOnlyDictionary<SectionKey, SectionAggregate> losses)
    {
        var closing = opening.ToDictionary(
            entry => entry.Key,
            entry => new SectionAggregate
            {
                ProofGallons = entry.Value.ProofGallons,
                WineGallons = entry.Value.WineGallons
            },
            new SectionKeyComparer());

        ApplyAggregate(closing, production);
        ApplyAggregate(closing, transfersIn);
        ApplyAggregate(closing, transfersOut, invert: true);
        ApplyAggregate(closing, losses, invert: true);

        return closing;
    }

    private static SectionAggregate SumAggregate(IReadOnlyDictionary<SectionKey, SectionAggregate> aggregate)
    {
        var total = new SectionAggregate();

        foreach (var value in aggregate.Values)
        {
            total.ProofGallons += value.ProofGallons;
            total.WineGallons += value.WineGallons;
        }

        return total;
    }

    private static decimal CalculateBarrelsFromWineGallons(decimal wineGallons)
    {
        // TTB standard barrel conversion
        // See docs/TTB_COMPLIANCE_GUIDE.md (Never Modify These Constants)
        return Math.Round(wineGallons / StandardBarrelWineGallons, 2, MidpointRounding.AwayFromZero);
    }

    private static void ApplyAggregate(
        IDictionary<SectionKey, SectionAggregate> target,
        IReadOnlyDictionary<SectionKey, SectionAggregate> source,
        bool invert = false)
    {
        var multiplier = invert ? -1m : 1m;

        foreach (var (key, value) in source)
        {
            AddToAggregate(target, key, value.ProofGallons * multiplier, value.WineGallons * multiplier);
        }
    }

    private static void AddToAggregate(
        IDictionary<SectionKey, SectionAggregate> aggregate,
        SectionKey key,
        decimal proofGallons,
        decimal wineGallons)
    {
        if (!aggregate.TryGetValue(key, out var value))
        {
            value = new SectionAggregate();
            aggregate[key] = value;
        }

        value.ProofGallons += proofGallons;
        value.WineGallons += wineGallons;
    }

    private async Task<IReadOnlyCollection<WarehouseProofGallons>> BuildWarehouseProofGallonsAsync(
        int companyId,
        IReadOnlyDictionary<SectionKey, SectionAggregate> closing,
        IReadOnlyCollection<TtbTransaction> monthlyTransactions,
        CancellationToken cancellationToken)
    {
        var closingProofGallons = Math.Round(SumAggregate(closing).ProofGallons, 2, MidpointRounding.AwayFromZero);

        var warehouseTransactions = monthlyTransactions
            .Where(t => string.Equals(t.SourceEntityType, nameof(Rickhouse), StringComparison.OrdinalIgnoreCase)
                        && t.SourceEntityId.HasValue)
            .GroupBy(t => t.SourceEntityId!.Value)
            .ToDictionary(
                group => group.Key,
                group => Math.Round(group.Sum(t => t.ProofGallons * (decimal)GetTransactionMultiplier(t.TransactionType)), 2, MidpointRounding.AwayFromZero));

        var warehouses = await dbContext.Rickhouses
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        if (warehouseTransactions.Count > 0)
        {
            var warehouseTotals = new List<WarehouseProofGallons>();

            foreach (var (rickhouseId, proofGallons) in warehouseTransactions)
            {
                var warehouseName = warehouses.TryGetValue(rickhouseId, out var resolvedName)
                    ? resolvedName
                    : $"Warehouse {rickhouseId}";

                warehouseTotals.Add(new WarehouseProofGallons
                {
                    WarehouseName = warehouseName,
                    ProofGallons = proofGallons
                });
            }

            var unspecifiedProof = closingProofGallons - warehouseTotals.Sum(w => w.ProofGallons);
            if (Math.Abs(unspecifiedProof) > SnapshotTolerance)
            {
                warehouseTotals.Add(new WarehouseProofGallons
                {
                    WarehouseName = "Unspecified Warehouse",
                    ProofGallons = Math.Round(unspecifiedProof, 2, MidpointRounding.AwayFromZero)
                });
            }

            return warehouseTotals;
        }

        var label = warehouses.Count > 1
            ? "All Warehouses"
            : warehouses.Values.FirstOrDefault() ?? "Storage";

        return new[]
        {
            new WarehouseProofGallons
            {
                WarehouseName = label,
                ProofGallons = closingProofGallons
            }
        };
    }

    private static decimal GetTransactionMultiplier(TtbTransactionType transactionType) => transactionType switch
    {
        TtbTransactionType.Production => 1m,
        TtbTransactionType.TransferIn => 1m,
        TtbTransactionType.TransferOut => -1m,
        TtbTransactionType.Loss => -1m,
        TtbTransactionType.Gain => 1m,
        TtbTransactionType.Destruction => -1m,
        TtbTransactionType.Bottling => -1m,
        TtbTransactionType.TaxDetermination => -1m,
        _ => 0m
    };

    private sealed record SectionKey(string ProductType, TtbSpiritsType SpiritsType);

    private sealed class SectionAggregate
    {
        public decimal ProofGallons { get; set; }

        public decimal WineGallons { get; set; }
    }

    private sealed class SectionKeyComparer : IEqualityComparer<SectionKey>
    {
        public bool Equals(SectionKey? x, SectionKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.ProductType, y.ProductType, StringComparison.OrdinalIgnoreCase) && x.SpiritsType == y.SpiritsType;
        }

        public int GetHashCode(SectionKey obj) => HashCode.Combine(obj.ProductType.ToLowerInvariant(), obj.SpiritsType);
    }
}
