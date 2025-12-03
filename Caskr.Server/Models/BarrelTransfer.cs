using System;

namespace Caskr.server.Models;

/// <summary>
/// Links individual barrels to inter-warehouse transfers
/// </summary>
public class BarrelTransfer
{
    public int Id { get; set; }

    public int BarrelId { get; set; }

    public int TransferId { get; set; }

    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Barrel Barrel { get; set; } = null!;

    public virtual InterWarehouseTransfer Transfer { get; set; } = null!;
}
