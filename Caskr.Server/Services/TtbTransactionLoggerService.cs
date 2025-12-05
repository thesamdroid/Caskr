using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ITtbTransactionLogger
{
    Task LogProductionAsync(int batchId, DateTime productionDate);

    Task LogTransferInAsync(int transferId);

    Task LogTransferOutAsync(int transferId);

    Task LogLossAsync(int barrelId, decimal proofGallons, string reason);

    Task LogTaxDeterminationAsync(int orderId);
}

/// <summary>
///     Centralized service for creating <see cref="TtbTransaction"/> records whenever a TTB-relevant
///     event occurs. The service is intentionally defensive – missing metadata is treated as an error so
///     that upstream workflows can surface actionable diagnostics without silently dropping compliance data.
/// </summary>
public class TtbTransactionLoggerService(
    CaskrDbContext dbContext,
    ILogger<TtbTransactionLoggerService> logger) : ITtbTransactionLogger
{
    private const decimal GallonsPerBarrel = 53m;

    public async Task LogProductionAsync(int batchId, DateTime productionDate)
    {
        logger.LogInformation("Creating production transaction for batch {BatchId}", batchId);

        var batch = await dbContext.Batches
            .Include(b => b.MashBill)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} was not found.");

        if (await TransactionExistsAsync(TtbTransactionType.Production, nameof(Batch), batch.Id))
        {
            logger.LogInformation("Production transaction already exists for batch {BatchId}", batchId);
            return;
        }

        var metrics = await BuildBatchMetricsAsync(batch);
        var proofGallons = TtbVolumeCalculator.CalculateProofGallons(metrics.VolumeGallons, metrics.Metadata.Abv);

        await PersistTransactionAsync(
            batch.CompanyId,
            productionDate.Date,
            TtbTransactionType.Production,
            metrics.Metadata.ProductType,
            metrics.Metadata.SpiritsType,
            proofGallons,
            metrics.VolumeGallons,
            nameof(Batch),
            batch.Id,
            $"Batch {batch.Id} completed on {productionDate:yyyy-MM-dd}");
    }

    public Task LogTransferInAsync(int transferId) => LogTransferAsync(transferId, TtbTransactionType.TransferIn);

    public Task LogTransferOutAsync(int transferId) => LogTransferAsync(transferId, TtbTransactionType.TransferOut);

    public async Task LogLossAsync(int barrelId, decimal proofGallons, string reason)
    {
        logger.LogInformation("Recording loss for barrel {BarrelId}", barrelId);

        var barrel = await dbContext.Barrels
            .Include(b => b.Order)
                .ThenInclude(o => o.SpiritType)
            .Include(b => b.Batch)
                .ThenInclude(b => b!.MashBill)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == barrelId)
            ?? throw new InvalidOperationException($"Barrel {barrelId} was not found.");

        if (await TransactionExistsAsync(TtbTransactionType.Loss, nameof(Barrel), barrel.Id))
        {
            logger.LogInformation("Loss transaction already exists for barrel {BarrelId}", barrelId);
            return;
        }

        if (barrel.Order == null)
        {
            throw new InvalidOperationException($"Barrel {barrelId} is not associated with an order – cannot determine spirit type.");
        }

        var metadata = TtbProductMetadataCatalog.Resolve(barrel.Batch, barrel.Order, barrel);
        var normalizedProof = Math.Round(Math.Max(0, proofGallons), 2, MidpointRounding.AwayFromZero);
        var productProof = metadata.Abv * 2m;
        var wineGallons = productProof <= 0
            ? 0m
            : Math.Round(Math.Max(0, normalizedProof / (productProof / 100m)), 2, MidpointRounding.AwayFromZero);

        var lossNotes = string.IsNullOrWhiteSpace(reason)
            ? $"Loss recorded for barrel {barrel.Sku}"
            : $"Loss recorded for barrel {barrel.Sku}: {reason.Trim()}";

        await PersistTransactionAsync(
            barrel.CompanyId,
            DateTime.UtcNow,
            TtbTransactionType.Loss,
            metadata.ProductType,
            metadata.SpiritsType,
            normalizedProof,
            wineGallons,
            nameof(Barrel),
            barrel.Id,
            lossNotes);
    }

    public async Task LogTaxDeterminationAsync(int orderId)
    {
        logger.LogInformation("Recording tax determination for order {OrderId}", orderId);

        var order = await dbContext.Orders
            .Include(o => o.SpiritType)
            .Include(o => o.Batch)
                .ThenInclude(b => b!.MashBill)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException($"Order {orderId} was not found.");

        if (await TransactionExistsAsync(TtbTransactionType.TaxDetermination, nameof(Order), order.Id))
        {
            logger.LogInformation("Tax determination transaction already exists for order {OrderId}", orderId);
            return;
        }

        var metrics = ResolveOrderMetrics(order);
        var proofGallons = TtbVolumeCalculator.CalculateProofGallons(metrics.VolumeGallons, metrics.Metadata.Abv);

        await PersistTransactionAsync(
            order.CompanyId,
            DateTime.UtcNow,
            TtbTransactionType.TaxDetermination,
            metrics.Metadata.ProductType,
            metrics.Metadata.SpiritsType,
            proofGallons,
            metrics.VolumeGallons,
            nameof(Order),
            order.Id,
            $"Tax determination for order {order.Name}");
    }

    private async Task LogTransferAsync(int transferId, TtbTransactionType transactionType)
    {
        logger.LogInformation("Logging {TransactionType} for transfer {TransferId}", transactionType, transferId);

        var order = await dbContext.Orders
            .Include(o => o.SpiritType)
            .Include(o => o.Batch)
                .ThenInclude(b => b!.MashBill)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == transferId)
            ?? throw new InvalidOperationException($"Transfer {transferId} is not available. Transfers are represented by orders until a dedicated aggregate exists.");

        if (await TransactionExistsAsync(transactionType, "Transfer", transferId))
        {
            logger.LogInformation("{TransactionType} transaction already exists for transfer {TransferId}", transactionType, transferId);
            return;
        }

        var metrics = ResolveOrderMetrics(order);
        var proofGallons = TtbVolumeCalculator.CalculateProofGallons(metrics.VolumeGallons, metrics.Metadata.Abv);
        var direction = transactionType == TtbTransactionType.TransferIn ? "received" : "sent";

        await PersistTransactionAsync(
            order.CompanyId,
            DateTime.UtcNow,
            transactionType,
            metrics.Metadata.ProductType,
            metrics.Metadata.SpiritsType,
            proofGallons,
            metrics.VolumeGallons,
            "Transfer",
            transferId,
            $"Transfer {direction} using order {order.Id}");
    }

    private async Task<bool> TransactionExistsAsync(TtbTransactionType transactionType, string sourceEntityType, int sourceEntityId)
    {
        return await dbContext.TtbTransactions
            .AsNoTracking()
            .AnyAsync(t => t.TransactionType == transactionType
                && t.SourceEntityType == sourceEntityType
                && t.SourceEntityId == sourceEntityId);
    }

    private async Task PersistTransactionAsync(
        int companyId,
        DateTime transactionDate,
        TtbTransactionType transactionType,
        string productType,
        TtbSpiritsType spiritsType,
        decimal proofGallons,
        decimal wineGallons,
        string sourceEntityType,
        int sourceEntityId,
        string? notes)
    {
        if (companyId <= 0)
        {
            throw new InvalidOperationException("A valid company identifier is required to persist a TTB transaction.");
        }

        var entity = new TtbTransaction
        {
            CompanyId = companyId,
            TransactionDate = transactionDate.Date,
            TransactionType = transactionType,
            ProductType = productType,
            SpiritsType = spiritsType,
            ProofGallons = Math.Round(Math.Max(0, proofGallons), 2, MidpointRounding.AwayFromZero),
            WineGallons = Math.Round(Math.Max(0, wineGallons), 2, MidpointRounding.AwayFromZero),
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            Notes = notes
        };

        await dbContext.TtbTransactions.AddAsync(entity);
        await dbContext.SaveChangesAsync();
    }

    private async Task<BatchMetrics> BuildBatchMetricsAsync(Batch batch)
    {
        var relatedOrders = await dbContext.Orders
            .Include(o => o.SpiritType)
            .Where(o => o.BatchId == batch.Id && o.CompanyId == batch.CompanyId)
            .AsNoTracking()
            .ToListAsync();

        if (relatedOrders.Count == 0)
        {
            throw new InvalidOperationException($"Batch {batch.Id} does not have any orders to derive production metrics from.");
        }

        var referenceOrder = relatedOrders.FirstOrDefault(o => o.SpiritType != null) ?? relatedOrders.First();
        var metadata = TtbProductMetadataCatalog.Resolve(batch, referenceOrder, null);

        var totalBarrels = relatedOrders.Sum(o => (decimal)o.Quantity);
        if (totalBarrels <= 0)
        {
            throw new InvalidOperationException($"Orders linked to batch {batch.Id} do not contain a positive quantity.");
        }

        var volumeGallons = totalBarrels * GallonsPerBarrel;
        return new BatchMetrics(metadata, volumeGallons);
    }

    private OrderMetrics ResolveOrderMetrics(Order order)
    {
        var metadata = TtbProductMetadataCatalog.Resolve(order.Batch, order, null);
        if (order.Quantity <= 0)
        {
            throw new InvalidOperationException($"Order {order.Id} does not have a positive quantity and cannot be translated into gallons.");
        }

        var volume = order.Quantity * GallonsPerBarrel;
        return new OrderMetrics(metadata, volume);
    }

    private sealed record BatchMetrics(ProductMetadata Metadata, decimal VolumeGallons);

    private sealed record OrderMetrics(ProductMetadata Metadata, decimal VolumeGallons);
}
