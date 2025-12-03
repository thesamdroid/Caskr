using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

/// <summary>
/// Represents a storage facility (rickhouse, palletized warehouse, tank farm, or outdoor storage)
/// for barrel inventory. Each company can have multiple warehouses.
/// </summary>
public class Warehouse
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public WarehouseType WarehouseType { get; set; } = WarehouseType.Rickhouse;

    // Address fields
    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "USA";

    // Capacity and dimensions
    /// <summary>
    /// Maximum number of barrel positions in this warehouse
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Length of the warehouse in feet
    /// </summary>
    public decimal? LengthFeet { get; set; }

    /// <summary>
    /// Width of the warehouse in feet
    /// </summary>
    public decimal? WidthFeet { get; set; }

    /// <summary>
    /// Height of the warehouse in feet
    /// </summary>
    public decimal? HeightFeet { get; set; }

    // Status and metadata
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();

    public virtual ICollection<InterWarehouseTransfer> OutgoingTransfers { get; set; } = new List<InterWarehouseTransfer>();

    public virtual ICollection<InterWarehouseTransfer> IncomingTransfers { get; set; } = new List<InterWarehouseTransfer>();

    public virtual ICollection<Order> FulfillmentOrders { get; set; } = new List<Order>();

    public virtual ICollection<WarehouseCapacitySnapshot> CapacitySnapshots { get; set; } = new List<WarehouseCapacitySnapshot>();

    // Computed property for current occupancy
    public int OccupiedPositions => Barrels?.Count ?? 0;

    public decimal OccupancyPercentage => TotalCapacity > 0
        ? Math.Round((decimal)OccupiedPositions / TotalCapacity * 100, 2)
        : 0;

    // Helper for full address
    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(AddressLine1)) parts.Add(AddressLine1);
            if (!string.IsNullOrWhiteSpace(AddressLine2)) parts.Add(AddressLine2);

            var cityStateZip = new List<string>();
            if (!string.IsNullOrWhiteSpace(City)) cityStateZip.Add(City);
            if (!string.IsNullOrWhiteSpace(State)) cityStateZip.Add(State);
            if (!string.IsNullOrWhiteSpace(PostalCode)) cityStateZip.Add(PostalCode);

            if (cityStateZip.Count > 0) parts.Add(string.Join(", ", cityStateZip));
            if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country);

            return string.Join(", ", parts);
        }
    }
}
