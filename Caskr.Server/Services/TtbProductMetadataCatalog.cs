using System;
using System.Collections.Generic;
using Caskr.server.Models;

namespace Caskr.server.Services;

internal static class TtbProductMetadataCatalog
{
    private static readonly IReadOnlyDictionary<string, SpiritTypeMetadata> SpiritTypeLookups =
        new Dictionary<string, SpiritTypeMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bourbon"] = new(TtbSpiritsType.Under190Proof, 62.5m),
            ["Whiskey"] = new(TtbSpiritsType.Under190Proof, 62.5m),
            ["Rye"] = new(TtbSpiritsType.Under190Proof, 62.5m),
            ["Gin"] = new(TtbSpiritsType.Under190Proof, 70m),
            ["Vodka"] = new(TtbSpiritsType.Neutral190OrMore, 95m),
            ["Neutral"] = new(TtbSpiritsType.Neutral190OrMore, 95m),
            ["Tequila"] = new(TtbSpiritsType.Under190Proof, 80m),
            ["Rum"] = new(TtbSpiritsType.Under190Proof, 80m),
            ["Brandy"] = new(TtbSpiritsType.Wine, 40m),
            ["Wine"] = new(TtbSpiritsType.Wine, 20m)
        };

    public static ProductMetadata Resolve(Batch? batch, Order? order, Barrel? barrel)
    {
        var productType = DetermineProductType(batch, order, barrel);
        var spiritKey = order?.SpiritType?.Name ?? productType;
        var spiritMetadata = ResolveSpiritsMetadata(spiritKey);
        return new ProductMetadata(productType, spiritMetadata.SpiritsType, spiritMetadata.Abv);
    }

    private static string DetermineProductType(Batch? batch, Order? order, Barrel? barrel)
    {
        if (!string.IsNullOrWhiteSpace(order?.SpiritType?.Name))
        {
            return order!.SpiritType!.Name;
        }

        if (!string.IsNullOrWhiteSpace(batch?.MashBill?.Name))
        {
            return batch!.MashBill!.Name;
        }

        return barrel?.Sku ?? $"Batch {batch?.Id ?? 0}";
    }

    private static SpiritTypeMetadata ResolveSpiritsMetadata(string? spiritName)
    {
        if (!string.IsNullOrWhiteSpace(spiritName) && SpiritTypeLookups.TryGetValue(spiritName, out var metadata))
        {
            return metadata;
        }

        return new SpiritTypeMetadata(TtbSpiritsType.Under190Proof, 80m);
    }
}

internal readonly record struct SpiritTypeMetadata(TtbSpiritsType SpiritsType, decimal Abv);

internal readonly record struct ProductMetadata(string ProductType, TtbSpiritsType SpiritsType, decimal Abv);
