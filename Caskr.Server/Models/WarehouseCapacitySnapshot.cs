using System;

namespace Caskr.server.Models;

/// <summary>
/// Daily snapshot of warehouse occupancy for capacity trending and reporting
/// </summary>
public class WarehouseCapacitySnapshot
{
    public int Id { get; set; }

    public int WarehouseId { get; set; }

    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// Total capacity at time of snapshot
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Number of occupied barrel positions at time of snapshot
    /// </summary>
    public int OccupiedPositions { get; set; }

    /// <summary>
    /// Percentage of capacity occupied (0-100)
    /// </summary>
    public decimal OccupancyPercentage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Warehouse Warehouse { get; set; } = null!;
}
