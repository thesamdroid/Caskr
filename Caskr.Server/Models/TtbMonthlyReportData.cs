using System;
using System.Collections.Generic;
using Caskr.server.Models;

namespace Caskr.server.Models;

public sealed class TtbMonthlyReportData
{
    public int CompanyId { get; init; }

    public int Month { get; init; }

    public int Year { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public InventorySection OpeningInventory { get; init; } = new();

    public ProductionSection Production { get; init; } = new();

    public TransfersSection Transfers { get; init; } = new();

    public LossSection Losses { get; init; } = new();

    public InventorySection ClosingInventory { get; init; } = new();
}

public sealed class InventorySection
{
    public IReadOnlyCollection<TtbSectionTotal> Rows { get; init; } = Array.Empty<TtbSectionTotal>();
}

public sealed class ProductionSection
{
    public IReadOnlyCollection<TtbSectionTotal> Rows { get; init; } = Array.Empty<TtbSectionTotal>();
}

public sealed class TransfersSection
{
    public IReadOnlyCollection<TtbSectionTotal> TransfersIn { get; init; } = Array.Empty<TtbSectionTotal>();

    public IReadOnlyCollection<TtbSectionTotal> TransfersOut { get; init; } = Array.Empty<TtbSectionTotal>();
}

public sealed class LossSection
{
    public IReadOnlyCollection<TtbSectionTotal> Rows { get; init; } = Array.Empty<TtbSectionTotal>();
}

public sealed class TtbSectionTotal
{
    public required string ProductType { get; init; }

    public required TtbSpiritsType SpiritsType { get; init; }

    public decimal ProofGallons { get; init; }

    public decimal WineGallons { get; init; }
}
