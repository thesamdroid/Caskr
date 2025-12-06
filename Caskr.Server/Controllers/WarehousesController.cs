using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Request model for creating/updating a warehouse
/// </summary>
public class WarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public WarehouseType WarehouseType { get; set; } = WarehouseType.Rickhouse;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "USA";
    public int TotalCapacity { get; set; }
    public decimal? LengthFeet { get; set; }
    public decimal? WidthFeet { get; set; }
    public decimal? HeightFeet { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Response model for warehouse with occupancy data
/// </summary>
public class WarehouseResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = "Rickhouse";
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public int TotalCapacity { get; set; }
    public decimal? LengthFeet { get; set; }
    public decimal? WidthFeet { get; set; }
    public decimal? HeightFeet { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    // Occupancy data
    public int OccupiedPositions { get; set; }
    public decimal OccupancyPercentage { get; set; }
    public int AvailablePositions { get; set; }
}

/// <summary>
/// Controller for warehouse management - create, read, update, deactivate warehouses
/// </summary>
public class WarehousesController : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<WarehousesController> _logger;
    private readonly IUsersService _usersService;

    public WarehousesController(CaskrDbContext dbContext, ILogger<WarehousesController> logger, IUsersService usersService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _usersService = usersService;
    }

    /// <summary>
    /// Get all warehouses for a company
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<WarehouseResponse>>> GetWarehouses(
        int companyId,
        [FromQuery] bool includeInactive = false)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Fetching warehouses for company {CompanyId}", companyId);

        var query = _dbContext.Warehouses
            .Include(w => w.Barrels)
            .Include(w => w.CreatedByUser)
            .Where(w => w.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(w => w.IsActive);
        }

        var warehouses = await query
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseResponse
            {
                Id = w.Id,
                CompanyId = w.CompanyId,
                Name = w.Name,
                WarehouseType = w.WarehouseType.ToString(),
                AddressLine1 = w.AddressLine1,
                AddressLine2 = w.AddressLine2,
                City = w.City,
                State = w.State,
                PostalCode = w.PostalCode,
                Country = w.Country,
                FullAddress = w.FullAddress,
                TotalCapacity = w.TotalCapacity,
                LengthFeet = w.LengthFeet,
                WidthFeet = w.WidthFeet,
                HeightFeet = w.HeightFeet,
                IsActive = w.IsActive,
                Notes = w.Notes,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                CreatedByUserId = w.CreatedByUserId,
                CreatedByUserName = w.CreatedByUser != null ? w.CreatedByUser.Name : null,
                OccupiedPositions = w.Barrels.Count,
                OccupancyPercentage = w.TotalCapacity > 0
                    ? Math.Round((decimal)w.Barrels.Count / w.TotalCapacity * 100, 2)
                    : 0,
                AvailablePositions = Math.Max(0, w.TotalCapacity - w.Barrels.Count)
            })
            .ToListAsync();

        return Ok(warehouses);
    }

    /// <summary>
    /// Get a single warehouse by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WarehouseResponse>> GetWarehouse(int id)
    {
        var warehouse = await _dbContext.Warehouses
            .Include(w => w.Barrels)
            .Include(w => w.CreatedByUser)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != warehouse.CompanyId))
        {
            return Forbid();
        }

        return Ok(new WarehouseResponse
        {
            Id = warehouse.Id,
            CompanyId = warehouse.CompanyId,
            Name = warehouse.Name,
            WarehouseType = warehouse.WarehouseType.ToString(),
            AddressLine1 = warehouse.AddressLine1,
            AddressLine2 = warehouse.AddressLine2,
            City = warehouse.City,
            State = warehouse.State,
            PostalCode = warehouse.PostalCode,
            Country = warehouse.Country,
            FullAddress = warehouse.FullAddress,
            TotalCapacity = warehouse.TotalCapacity,
            LengthFeet = warehouse.LengthFeet,
            WidthFeet = warehouse.WidthFeet,
            HeightFeet = warehouse.HeightFeet,
            IsActive = warehouse.IsActive,
            Notes = warehouse.Notes,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt,
            CreatedByUserId = warehouse.CreatedByUserId,
            CreatedByUserName = warehouse.CreatedByUser?.Name,
            OccupiedPositions = warehouse.Barrels.Count,
            OccupancyPercentage = warehouse.TotalCapacity > 0
                ? Math.Round((decimal)warehouse.Barrels.Count / warehouse.TotalCapacity * 100, 2)
                : 0,
            AvailablePositions = Math.Max(0, warehouse.TotalCapacity - warehouse.Barrels.Count)
        });
    }

    /// <summary>
    /// Create a new warehouse
    /// </summary>
    [HttpPost("company/{companyId}")]
    public async Task<ActionResult<WarehouseResponse>> CreateWarehouse(int companyId, [FromBody] WarehouseRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Warehouse name is required" });
        }

        // Check for duplicate name within company
        var existingWarehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Name == request.Name);

        if (existingWarehouse is not null)
        {
            return BadRequest(new { message = "A warehouse with this name already exists" });
        }

        var warehouse = new Warehouse
        {
            CompanyId = companyId,
            Name = request.Name,
            WarehouseType = request.WarehouseType,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country ?? "USA",
            TotalCapacity = request.TotalCapacity,
            LengthFeet = request.LengthFeet,
            WidthFeet = request.WidthFeet,
            HeightFeet = request.HeightFeet,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created warehouse {WarehouseId} '{WarehouseName}' for company {CompanyId} by user {UserId}",
            warehouse.Id, warehouse.Name?.Replace("\r", "").Replace("\n", ""), companyId, user.Id);

        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, new WarehouseResponse
        {
            Id = warehouse.Id,
            CompanyId = warehouse.CompanyId,
            Name = warehouse.Name,
            WarehouseType = warehouse.WarehouseType.ToString(),
            AddressLine1 = warehouse.AddressLine1,
            AddressLine2 = warehouse.AddressLine2,
            City = warehouse.City,
            State = warehouse.State,
            PostalCode = warehouse.PostalCode,
            Country = warehouse.Country,
            FullAddress = warehouse.FullAddress,
            TotalCapacity = warehouse.TotalCapacity,
            LengthFeet = warehouse.LengthFeet,
            WidthFeet = warehouse.WidthFeet,
            HeightFeet = warehouse.HeightFeet,
            IsActive = warehouse.IsActive,
            Notes = warehouse.Notes,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt,
            CreatedByUserId = warehouse.CreatedByUserId,
            CreatedByUserName = user.Name,
            OccupiedPositions = 0,
            OccupancyPercentage = 0,
            AvailablePositions = warehouse.TotalCapacity
        });
    }

    /// <summary>
    /// Update a warehouse
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WarehouseResponse>> UpdateWarehouse(int id, [FromBody] WarehouseRequest request)
    {
        var warehouse = await _dbContext.Warehouses
            .Include(w => w.Barrels)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != warehouse.CompanyId))
        {
            return Forbid();
        }

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Warehouse name is required" });
        }

        // Check for duplicate name within company (excluding this warehouse)
        var existingWarehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.CompanyId == warehouse.CompanyId && w.Name == request.Name && w.Id != id);

        if (existingWarehouse is not null)
        {
            return BadRequest(new { message = "A warehouse with this name already exists" });
        }

        warehouse.Name = request.Name;
        warehouse.WarehouseType = request.WarehouseType;
        warehouse.AddressLine1 = request.AddressLine1;
        warehouse.AddressLine2 = request.AddressLine2;
        warehouse.City = request.City;
        warehouse.State = request.State;
        warehouse.PostalCode = request.PostalCode;
        warehouse.Country = request.Country ?? "USA";
        warehouse.TotalCapacity = request.TotalCapacity;
        warehouse.LengthFeet = request.LengthFeet;
        warehouse.WidthFeet = request.WidthFeet;
        warehouse.HeightFeet = request.HeightFeet;
        warehouse.Notes = request.Notes;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Updated warehouse {WarehouseId} '{WarehouseName}' by user {UserId}",
            warehouse.Id, warehouse.Name, user.Id);

        return Ok(new WarehouseResponse
        {
            Id = warehouse.Id,
            CompanyId = warehouse.CompanyId,
            Name = warehouse.Name,
            WarehouseType = warehouse.WarehouseType.ToString(),
            AddressLine1 = warehouse.AddressLine1,
            AddressLine2 = warehouse.AddressLine2,
            City = warehouse.City,
            State = warehouse.State,
            PostalCode = warehouse.PostalCode,
            Country = warehouse.Country,
            FullAddress = warehouse.FullAddress,
            TotalCapacity = warehouse.TotalCapacity,
            LengthFeet = warehouse.LengthFeet,
            WidthFeet = warehouse.WidthFeet,
            HeightFeet = warehouse.HeightFeet,
            IsActive = warehouse.IsActive,
            Notes = warehouse.Notes,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt,
            CreatedByUserId = warehouse.CreatedByUserId,
            OccupiedPositions = warehouse.Barrels.Count,
            OccupancyPercentage = warehouse.TotalCapacity > 0
                ? Math.Round((decimal)warehouse.Barrels.Count / warehouse.TotalCapacity * 100, 2)
                : 0,
            AvailablePositions = Math.Max(0, warehouse.TotalCapacity - warehouse.Barrels.Count)
        });
    }

    /// <summary>
    /// Deactivate a warehouse (soft delete)
    /// Cannot deactivate if barrels are still stored there
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateWarehouse(int id)
    {
        var warehouse = await _dbContext.Warehouses
            .Include(w => w.Barrels)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != warehouse.CompanyId))
        {
            return Forbid();
        }

        // Check if warehouse has barrels
        if (warehouse.Barrels.Count > 0)
        {
            return BadRequest(new
            {
                message = $"Cannot deactivate warehouse. There are still {warehouse.Barrels.Count} barrel(s) stored in this warehouse. Transfer them first."
            });
        }

        warehouse.IsActive = false;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Deactivated warehouse {WarehouseId} '{WarehouseName}' by user {UserId}",
            warehouse.Id, warehouse.Name, user.Id);

        return Ok(new { message = "Warehouse deactivated successfully" });
    }

    /// <summary>
    /// Reactivate a deactivated warehouse
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateWarehouse(int id)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != warehouse.CompanyId))
        {
            return Forbid();
        }

        warehouse.IsActive = true;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Activated warehouse {WarehouseId} '{WarehouseName}' by user {UserId}",
            warehouse.Id, warehouse.Name, user.Id);

        return Ok(new { message = "Warehouse activated successfully" });
    }

    /// <summary>
    /// Get warehouse capacity snapshots for trending
    /// </summary>
    [HttpGet("{id}/capacity-history")]
    public async Task<ActionResult<IEnumerable<WarehouseCapacitySnapshot>>> GetCapacityHistory(
        int id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var warehouse = await _dbContext.Warehouses.FindAsync(id);

        if (warehouse is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != warehouse.CompanyId))
        {
            return Forbid();
        }

        var query = _dbContext.WarehouseCapacitySnapshots
            .Where(s => s.WarehouseId == id);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.SnapshotDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.SnapshotDate <= endDate.Value);
        }

        var snapshots = await query
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        return Ok(snapshots);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        return await GetCurrentUserAsync(_usersService);
    }
}
