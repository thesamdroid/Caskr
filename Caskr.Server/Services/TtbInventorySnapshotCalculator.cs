using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ITtbInventorySnapshotCalculator
{
    Task<IReadOnlyList<TtbInventorySnapshot>> BuildSnapshotRowsAsync(
        int companyId,
        DateTime snapshotDate,
        CancellationToken cancellationToken);
}

public sealed class TtbInventorySnapshotCalculator(
    CaskrDbContext dbContext,
    ILogger<TtbInventorySnapshotCalculator> logger) : ITtbInventorySnapshotCalculator
{
    private const decimal GallonsPerBarrel = 53m;

    private static readonly HashSet<string> InactiveOrderStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "sold",
        "emptied",
        "dumped",
        "transferred out"
    };

    public async Task<IReadOnlyList<TtbInventorySnapshot>> BuildSnapshotRowsAsync(
        int companyId,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var normalizedDate = snapshotDate.Date;
        var snapshotUpperBoundExclusive = normalizedDate.AddDays(1);
        var barrels = await dbContext.Barrels
            .AsNoTracking()
            .Include(b => b.Order)
                .ThenInclude(o => o.Status)
            .Include(b => b.Order)
                .ThenInclude(o => o.SpiritType)
            .Include(b => b.Batch)
                .ThenInclude(batch => batch!.MashBill)
            .Where(b => b.CompanyId == companyId && b.Order.CreatedAt < snapshotUpperBoundExclusive)
            .ToListAsync(cancellationToken);

        if (barrels.Count == 0)
        {
            logger.LogDebug(
                "No barrels available for company {CompanyId}; skipping TTB inventory snapshot calculation for {Date}.",
                companyId,
                normalizedDate);

            return Array.Empty<TtbInventorySnapshot>();
        }

        var aggregates = new Dictionary<InventoryGroupKey, InventoryAggregate>(new InventoryGroupKeyComparer());

        foreach (var barrel in barrels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (barrel.Order is null)
            {
                logger.LogWarning(
                    "Barrel {BarrelId} in company {CompanyId} has no associated order; it will be excluded from the snapshot.",
                    barrel.Id,
                    companyId);
                continue;
            }

            if (!ShouldIncludeBarrel(barrel.Order, snapshotUpperBoundExclusive))
            {
                continue;
            }

            var classification = TtbProductMetadataCatalog.Resolve(barrel.Batch, barrel.Order, barrel);
            var key = new InventoryGroupKey(
                classification.ProductType,
                classification.SpiritsType,
                DetermineTaxStatus(barrel.Order));

            var wineGallons = GallonsPerBarrel;
            var proofGallons = TtbVolumeCalculator.CalculateProofGallons(wineGallons, classification.Abv);

            if (!aggregates.TryGetValue(key, out var aggregate))
            {
                aggregate = new InventoryAggregate();
                aggregates[key] = aggregate;
            }

            aggregate.WineGallons += wineGallons;
            aggregate.ProofGallons += proofGallons;
        }

        return aggregates.Count == 0
            ? Array.Empty<TtbInventorySnapshot>()
            : BuildSnapshots(companyId, normalizedDate, aggregates);
    }

    private static IReadOnlyList<TtbInventorySnapshot> BuildSnapshots(
        int companyId,
        DateTime snapshotDate,
        IReadOnlyDictionary<InventoryGroupKey, InventoryAggregate> aggregates)
    {
        var snapshots = new List<TtbInventorySnapshot>(aggregates.Count);

        foreach (var (key, aggregate) in aggregates)
        {
            snapshots.Add(new TtbInventorySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = snapshotDate,
                ProductType = key.ProductType,
                SpiritsType = key.SpiritsType,
                TaxStatus = key.TaxStatus,
                WineGallons = Math.Round(aggregate.WineGallons, 2, MidpointRounding.AwayFromZero),
                ProofGallons = Math.Round(aggregate.ProofGallons, 2, MidpointRounding.AwayFromZero)
            });
        }

        return snapshots;
    }

    private static bool ShouldIncludeBarrel(Order order, DateTime snapshotUpperBoundExclusive)
    {
        if (order.CreatedAt >= snapshotUpperBoundExclusive)
        {
            return false;
        }

        var statusName = order.Status?.Name;
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return true;
        }

        var normalizedStatus = statusName.Trim();
        if (!InactiveOrderStatuses.Contains(normalizedStatus))
        {
            return true;
        }

        return order.UpdatedAt >= snapshotUpperBoundExclusive;
    }

    private static TtbTaxStatus DetermineTaxStatus(Order order)
    {
        var statusName = order.Status?.Name;
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return TtbTaxStatus.Bonded;
        }

        var normalized = statusName.Trim();
        if (normalized.Contains("tax paid", StringComparison.OrdinalIgnoreCase))
        {
            return TtbTaxStatus.TaxPaid;
        }

        if (normalized.Contains("export", StringComparison.OrdinalIgnoreCase))
        {
            return TtbTaxStatus.Export;
        }

        if (normalized.Contains("tax free", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("duty free", StringComparison.OrdinalIgnoreCase))
        {
            return TtbTaxStatus.TaxFree;
        }

        return TtbTaxStatus.Bonded;
    }

    private sealed record InventoryGroupKey(string ProductType, TtbSpiritsType SpiritsType, TtbTaxStatus TaxStatus);

    private sealed class InventoryAggregate
    {
        public decimal WineGallons { get; set; }

        public decimal ProofGallons { get; set; }
    }

    private sealed class InventoryGroupKeyComparer : IEqualityComparer<InventoryGroupKey>
    {
        public bool Equals(InventoryGroupKey? x, InventoryGroupKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.ProductType.Equals(y.ProductType, StringComparison.OrdinalIgnoreCase)
                && x.SpiritsType == y.SpiritsType
                && x.TaxStatus == y.TaxStatus;
        }

        public int GetHashCode(InventoryGroupKey obj)
        {
            return HashCode.Combine(
                obj.ProductType.ToLowerInvariant(),
                obj.SpiritsType,
                obj.TaxStatus);
        }
    }
}
