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
}

public sealed class TtbReportCalculatorService(
    CaskrDbContext dbContext,
    ILogger<TtbReportCalculatorService> logger) : ITtbReportCalculator
{
    private const decimal SnapshotTolerance = 0.01m;

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

    private static decimal GetTransactionMultiplier(TtbTransactionType transactionType) => transactionType switch
    {
        TtbTransactionType.Production => 1m,
        TtbTransactionType.TransferIn => 1m,
        TtbTransactionType.TransferOut => -1m,
        TtbTransactionType.Loss => -1m,
        TtbTransactionType.Gain => 1m,
        TtbTransactionType.Destruction => -1m,
        TtbTransactionType.Bottling => -1m,
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
