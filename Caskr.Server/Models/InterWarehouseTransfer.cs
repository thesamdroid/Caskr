using System;
using System.Collections.Generic;

namespace Caskr.server.Models;

/// <summary>
/// Represents a bulk transfer of barrels between two warehouses
/// </summary>
public class InterWarehouseTransfer
{
    public int Id { get; set; }

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public DateTime TransferDate { get; set; }

    /// <summary>
    /// Number of barrels in this transfer (auto-calculated from BarrelTransfers)
    /// </summary>
    public int BarrelsCount { get; set; }

    /// <summary>
    /// Total proof gallons being transferred
    /// </summary>
    public decimal? ProofGallons { get; set; }

    public WarehouseTransferStatus Status { get; set; } = WarehouseTransferStatus.Pending;

    public int? InitiatedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Warehouse FromWarehouse { get; set; } = null!;

    public virtual Warehouse ToWarehouse { get; set; } = null!;

    public virtual User? InitiatedByUser { get; set; }

    public virtual ICollection<BarrelTransfer> BarrelTransfers { get; set; } = new List<BarrelTransfer>();
}
