namespace Caskr.server.Models;

/// <summary>
/// Status of an inter-warehouse transfer
/// </summary>
public enum WarehouseTransferStatus
{
    /// <summary>Transfer has been created but not yet started</summary>
    Pending,

    /// <summary>Barrels are being moved between warehouses</summary>
    In_Transit,

    /// <summary>Transfer has been completed successfully</summary>
    Completed,

    /// <summary>Transfer was cancelled before completion</summary>
    Cancelled
}
