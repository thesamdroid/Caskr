using Caskr.server.Models;
using Caskr.server.Models.Portal;
using Caskr.server.Services.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Controllers;

/// <summary>
/// API controller for portal users to access their barrel investments.
/// </summary>
[ApiController]
[Route("api/portal/barrels")]
[Authorize]
public class PortalBarrelsController : ControllerBase
{
    private readonly CaskrDbContext _dbContext;
    private readonly IPortalAuthService _portalAuthService;
    private readonly ILogger<PortalBarrelsController> _logger;

    public PortalBarrelsController(
        CaskrDbContext dbContext,
        IPortalAuthService portalAuthService,
        ILogger<PortalBarrelsController> logger)
    {
        _dbContext = dbContext;
        _portalAuthService = portalAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Get all barrels owned by the current portal user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CaskOwnershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CaskOwnershipDto>>> GetMyBarrels()
    {
        var portalUserId = GetPortalUserId();
        if (portalUserId == null)
        {
            return Unauthorized(new { message = "Invalid portal user" });
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _portalAuthService.LogAccessAsync(
            portalUserId.Value,
            PortalAction.View_Dashboard,
            ipAddress: ipAddress,
            userAgent: userAgent);

        var ownerships = await _dbContext.CaskOwnerships
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Rickhouse)
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Batch)
                    .ThenInclude(b => b!.MashBill)
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Order)
                    .ThenInclude(o => o.SpiritType)
            .Include(o => o.Documents)
            .Where(o => o.PortalUserId == portalUserId.Value)
            .OrderByDescending(o => o.PurchaseDate)
            .ToListAsync();

        var result = ownerships.Select(MapToDto).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Get details of a specific barrel ownership
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CaskOwnershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaskOwnershipDto>> GetBarrelDetail(long id)
    {
        var portalUserId = GetPortalUserId();
        if (portalUserId == null)
        {
            return Unauthorized(new { message = "Invalid portal user" });
        }

        var ownership = await _dbContext.CaskOwnerships
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Rickhouse)
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Batch)
                    .ThenInclude(b => b!.MashBill)
            .Include(o => o.Barrel)
                .ThenInclude(b => b.Order)
                    .ThenInclude(o => o.SpiritType)
            .Include(o => o.Documents)
            .FirstOrDefaultAsync(o => o.Id == id && o.PortalUserId == portalUserId.Value);

        if (ownership == null)
        {
            return NotFound(new { message = "Barrel not found" });
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _portalAuthService.LogAccessAsync(
            portalUserId.Value,
            PortalAction.View_Barrel,
            resourceType: "Barrel",
            resourceId: ownership.BarrelId,
            ipAddress: ipAddress,
            userAgent: userAgent);

        return Ok(MapToDto(ownership));
    }

    private long? GetPortalUserId()
    {
        var portalUserIdClaim = User.FindFirst("portalUserId")?.Value;
        if (string.IsNullOrEmpty(portalUserIdClaim) || !long.TryParse(portalUserIdClaim, out var portalUserId))
        {
            return null;
        }
        return portalUserId;
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static CaskOwnershipDto MapToDto(CaskOwnership ownership)
    {
        return new CaskOwnershipDto
        {
            Id = ownership.Id,
            PurchaseDate = ownership.PurchaseDate,
            PurchasePrice = ownership.PurchasePrice,
            OwnershipPercentage = ownership.OwnershipPercentage,
            CertificateNumber = ownership.CertificateNumber,
            Status = ownership.Status.ToString(),
            Notes = ownership.Notes,
            CreatedAt = ownership.CreatedAt,
            Barrel = ownership.Barrel != null ? new BarrelDto
            {
                Id = ownership.Barrel.Id,
                Sku = ownership.Barrel.Sku,
                BatchId = ownership.Barrel.BatchId,
                CompanyId = ownership.Barrel.CompanyId,
                RickhouseId = ownership.Barrel.RickhouseId,
                Rickhouse = ownership.Barrel.Rickhouse != null ? new RickhouseDto
                {
                    Id = ownership.Barrel.Rickhouse.Id,
                    Name = ownership.Barrel.Rickhouse.Name,
                    Address = ownership.Barrel.Rickhouse.Address
                } : null,
                Batch = ownership.Barrel.Batch != null ? new BatchDto
                {
                    Id = ownership.Barrel.Batch.Id,
                    MashBill = ownership.Barrel.Batch.MashBill != null ? new MashBillDto
                    {
                        Id = ownership.Barrel.Batch.MashBill.Id,
                        Name = ownership.Barrel.Batch.MashBill.Name
                    } : null
                } : null,
                Order = ownership.Barrel.Order != null ? new OrderDto
                {
                    Id = ownership.Barrel.Order.Id,
                    Name = ownership.Barrel.Order.Name,
                    CreatedAt = ownership.Barrel.Order.CreatedAt,
                    SpiritType = ownership.Barrel.Order.SpiritType != null ? new SpiritTypeDto
                    {
                        Id = ownership.Barrel.Order.SpiritType.Id,
                        Name = ownership.Barrel.Order.SpiritType.Name
                    } : null
                } : null
            } : null,
            Documents = ownership.Documents?.Select(d => new PortalDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType.ToString(),
                FileName = d.FileName,
                FileSizeBytes = d.FileSizeBytes,
                MimeType = d.MimeType,
                UploadedAt = d.UploadedAt
            }).ToList() ?? new List<PortalDocumentDto>()
        };
    }
}

#region DTOs

public class CaskOwnershipDto
{
    public long Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public string? CertificateNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public BarrelDto? Barrel { get; set; }
    public List<PortalDocumentDto> Documents { get; set; } = new();
}

public class BarrelDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public int CompanyId { get; set; }
    public int RickhouseId { get; set; }
    public RickhouseDto? Rickhouse { get; set; }
    public BatchDto? Batch { get; set; }
    public OrderDto? Order { get; set; }
}

public class RickhouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class BatchDto
{
    public int Id { get; set; }
    public MashBillDto? MashBill { get; set; }
}

public class MashBillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OrderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public SpiritTypeDto? SpiritType { get; set; }
}

public class SpiritTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PortalDocumentDto
{
    public long Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public DateTime UploadedAt { get; set; }
}

#endregion
