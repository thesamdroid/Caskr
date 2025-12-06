using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Models.SupplyChain;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

#region Request/Response Models

public class SupplierRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public SupplierType SupplierType { get; set; } = SupplierType.Other;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
}

public class SupplierResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierType { get; set; } = "Other";
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SupplierProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public string? Sku { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public string? Notes { get; set; }
}

public class SupplierProductResponse
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public string? Sku { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

#endregion

/// <summary>
/// Controller for supplier management
/// </summary>
public class SuppliersController : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<SuppliersController> _logger;
    private readonly IUsersService _usersService;

    public SuppliersController(CaskrDbContext dbContext, ILogger<SuppliersController> logger, IUsersService usersService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _usersService = usersService;
    }

    /// <summary>
    /// Get all suppliers for the current user's company
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierResponse>>> GetSuppliersForCurrentCompany(
        [FromQuery] bool includeInactive = false)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        return await GetSuppliersInternal(user.CompanyId, includeInactive);
    }

    /// <summary>
    /// Get all suppliers for a specific company (SuperAdmin access)
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<SupplierResponse>>> GetSuppliers(
        int companyId,
        [FromQuery] bool includeInactive = false)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        return await GetSuppliersInternal(companyId, includeInactive);
    }

    private async Task<ActionResult<IEnumerable<SupplierResponse>>> GetSuppliersInternal(int companyId, bool includeInactive)
    {
        _logger.LogInformation("Fetching suppliers for company {CompanyId}", companyId);

        var query = _dbContext.Suppliers.Where(s => s.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var suppliers = await query
            .OrderBy(s => s.SupplierName)
            .Select(s => new SupplierResponse
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                SupplierName = s.SupplierName,
                SupplierType = s.SupplierType.ToString(),
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                Website = s.Website,
                PaymentTerms = s.PaymentTerms,
                IsActive = s.IsActive,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    /// <summary>
    /// Get a single supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierResponse>> GetSupplier(int id)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        return Ok(new SupplierResponse
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            SupplierName = supplier.SupplierName,
            SupplierType = supplier.SupplierType.ToString(),
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            Website = supplier.Website,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new supplier for the current user's company
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> CreateSupplierForCurrentCompany([FromBody] SupplierRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        return await CreateSupplierInternal(user.CompanyId, request);
    }

    /// <summary>
    /// Create a new supplier for a specific company (SuperAdmin access)
    /// </summary>
    [HttpPost("company/{companyId}")]
    public async Task<ActionResult<SupplierResponse>> CreateSupplier(int companyId, [FromBody] SupplierRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        return await CreateSupplierInternal(companyId, request);
    }

    private async Task<ActionResult<SupplierResponse>> CreateSupplierInternal(int companyId, SupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            return BadRequest(new { message = "Supplier name is required" });
        }

        // Check for duplicate name
        var existingSupplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.SupplierName == request.SupplierName);

        if (existingSupplier is not null)
        {
            return BadRequest(new { message = "A supplier with this name already exists" });
        }

        var supplier = new Supplier
        {
            CompanyId = companyId,
            SupplierName = request.SupplierName,
            SupplierType = request.SupplierType,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Website = request.Website,
            PaymentTerms = request.PaymentTerms,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created supplier {SupplierId} '{SupplierName}'",
            supplier.Id, SanitizeForLog(supplier.SupplierName));

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new SupplierResponse
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            SupplierName = supplier.SupplierName,
            SupplierType = supplier.SupplierType.ToString(),
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            Website = supplier.Website,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        });
    }

    /// <summary>
    /// Removes newlines and carriage returns to prevent log forging.
    /// </summary>
    private static string SanitizeForLog(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        // Remove \r, \n and trim whitespace
        return input.Replace("\r", "").Replace("\n", "").Trim();
    }

    /// <summary>
    /// Update a supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierResponse>> UpdateSupplier(int id, [FromBody] SupplierRequest request)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            return BadRequest(new { message = "Supplier name is required" });
        }

        // Check for duplicate name
        var existingSupplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyId == supplier.CompanyId && s.SupplierName == request.SupplierName && s.Id != id);

        if (existingSupplier is not null)
        {
            return BadRequest(new { message = "A supplier with this name already exists" });
        }

        supplier.SupplierName = request.SupplierName;
        supplier.SupplierType = request.SupplierType;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.Website = request.Website;
        supplier.PaymentTerms = request.PaymentTerms;
        supplier.Notes = request.Notes;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated supplier {SupplierId} '{SupplierName}'", supplier.Id, supplier.SupplierName);

        return Ok(new SupplierResponse
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            SupplierName = supplier.SupplierName,
            SupplierType = supplier.SupplierType.ToString(),
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            Website = supplier.Website,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        });
    }

    /// <summary>
    /// Deactivate a supplier
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateSupplier(int id)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deactivated supplier {SupplierId} '{SupplierName}'", supplier.Id, supplier.SupplierName);

        return Ok(new { message = "Supplier deactivated successfully" });
    }

    /// <summary>
    /// Activate a supplier
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateSupplier(int id)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        supplier.IsActive = true;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Activated supplier {SupplierId} '{SupplierName}'", supplier.Id, supplier.SupplierName);

        return Ok(new { message = "Supplier activated successfully" });
    }

    #region Supplier Products

    /// <summary>
    /// Get all products for a supplier
    /// </summary>
    [HttpGet("{supplierId}/products")]
    public async Task<ActionResult<IEnumerable<SupplierProductResponse>>> GetSupplierProducts(
        int supplierId,
        [FromQuery] bool includeInactive = false)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(supplierId);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        var query = _dbContext.SupplierProducts.Where(p => p.SupplierId == supplierId);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var products = await query
            .OrderBy(p => p.ProductName)
            .Select(p => new SupplierProductResponse
            {
                Id = p.Id,
                SupplierId = p.SupplierId,
                ProductName = p.ProductName,
                ProductCategory = p.ProductCategory,
                Sku = p.Sku,
                UnitOfMeasure = p.UnitOfMeasure,
                CurrentPrice = p.CurrentPrice,
                Currency = p.Currency,
                LeadTimeDays = p.LeadTimeDays,
                MinimumOrderQuantity = p.MinimumOrderQuantity,
                Notes = p.Notes,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Create a product for a supplier
    /// </summary>
    [HttpPost("{supplierId}/products")]
    public async Task<ActionResult<SupplierProductResponse>> CreateSupplierProduct(
        int supplierId,
        [FromBody] SupplierProductRequest request)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(supplierId);

        if (supplier is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != supplier.CompanyId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(new { message = "Product name is required" });
        }

        // Check for duplicate SKU if provided
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var existingProduct = await _dbContext.SupplierProducts
                .FirstOrDefaultAsync(p => p.SupplierId == supplierId && p.Sku == request.Sku);

            if (existingProduct is not null)
            {
                return BadRequest(new { message = "A product with this SKU already exists for this supplier" });
            }
        }

        var product = new SupplierProduct
        {
            SupplierId = supplierId,
            ProductName = request.ProductName,
            ProductCategory = request.ProductCategory,
            Sku = request.Sku,
            UnitOfMeasure = request.UnitOfMeasure,
            CurrentPrice = request.CurrentPrice,
            Currency = request.Currency,
            LeadTimeDays = request.LeadTimeDays,
            MinimumOrderQuantity = request.MinimumOrderQuantity,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.SupplierProducts.Add(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created product {ProductId} '{ProductName}' for supplier {SupplierId}",
            product.Id, product.ProductName, supplierId);

        return CreatedAtAction(nameof(GetSupplierProducts), new { supplierId }, new SupplierProductResponse
        {
            Id = product.Id,
            SupplierId = product.SupplierId,
            ProductName = product.ProductName,
            ProductCategory = product.ProductCategory,
            Sku = product.Sku,
            UnitOfMeasure = product.UnitOfMeasure,
            CurrentPrice = product.CurrentPrice,
            Currency = product.Currency,
            LeadTimeDays = product.LeadTimeDays,
            MinimumOrderQuantity = product.MinimumOrderQuantity,
            Notes = product.Notes,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        });
    }

    #endregion

    private async Task<User?> GetCurrentUserAsync()
    {
        return await GetCurrentUserAsync(_usersService);
    }
}
