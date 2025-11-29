using System;
using System.Collections.Generic;

namespace Caskr.server.Models;

public sealed class TtbForm5110_40Data
{
    public int CompanyId { get; init; }

    public int Month { get; init; }

    public int Year { get; init; }

    public decimal OpeningBarrels { get; init; }

    public decimal BarrelsReceived { get; init; }

    public decimal BarrelsRemoved { get; init; }

    public decimal ClosingBarrels { get; init; }

    public IReadOnlyCollection<WarehouseProofGallons> ProofGallonsByWarehouse { get; init; }
        = Array.Empty<WarehouseProofGallons>();
}

public sealed class WarehouseProofGallons
{
    public string WarehouseName { get; init; } = string.Empty;

    public decimal ProofGallons { get; init; }
}
