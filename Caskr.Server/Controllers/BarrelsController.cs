using System.Security.Claims;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

public class BarrelImportRequest
{
    public IFormFile? File { get; set; }

    public int? BatchId { get; set; }

    public int? MashBillId { get; set; }
}

public record BarrelImportError(string message, bool? requiresMashBillId = null);

public record BarrelImportSummary(int created, int batchId, bool createdNewBatch);
public class BarrelsController(IBarrelsService barrelsService, IUsersService usersService, ILogger<BarrelsController> logger)
    : AuthorizedApiControllerBase
{
    private readonly IBarrelsService _barrelsService = barrelsService;
    private readonly IUsersService _usersService = usersService;
    private readonly ILogger<BarrelsController> _logger = logger;

    /// <summary>
    /// Get barrels for the current user's company
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Barrel>>> GetBarrels()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        _logger.LogInformation("Fetching barrels for company {CompanyId}", user.CompanyId);
        var barrels = await _barrelsService.GetBarrelsForCompanyAsync(user.CompanyId);
        return Ok(barrels.ToList());
    }

    /// <summary>
    /// Get barrels for a specific company (SuperAdmin access)
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<Barrel>>> GetBarrelsForCompany(int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Fetching barrels for company {CompanyId}", companyId);
        var barrels = await _barrelsService.GetBarrelsForCompanyAsync(companyId);
        return Ok(barrels.ToList());
    }

    /// <summary>
    /// Forecast barrels for the current user's company
    /// </summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<object>> ForecastForCurrentCompany([FromQuery] DateTime targetDate, [FromQuery] int ageYears)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        _logger.LogInformation(
            "Forecasting barrels for company {CompanyId} with target date {TargetDate} and age {AgeYears}",
            user.CompanyId,
            targetDate,
            ageYears);
        var barrels = await _barrelsService.ForecastBarrelsAsync(user.CompanyId, targetDate, ageYears);
        var list = barrels.ToList();
        _logger.LogInformation(
            "Forecast complete for company {CompanyId}: {Count} barrels returned",
            user.CompanyId,
            list.Count);
        return Ok(new { barrels = list, count = list.Count });
    }

    /// <summary>
    /// Forecast barrels for a specific company (SuperAdmin access)
    /// </summary>
    [HttpGet("company/{companyId}/forecast")]
    public async Task<ActionResult<object>> Forecast(int companyId, [FromQuery] DateTime targetDate, [FromQuery] int ageYears)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Forecasting barrels for company {CompanyId} with target date {TargetDate} and age {AgeYears}",
            companyId,
            targetDate,
            ageYears);
        var barrels = await _barrelsService.ForecastBarrelsAsync(companyId, targetDate, ageYears);
        var list = barrels.ToList();
        _logger.LogInformation(
            "Forecast complete for company {CompanyId}: {Count} barrels returned",
            companyId,
            list.Count);
        return Ok(new { barrels = list, count = list.Count });
    }

    /// <summary>
    /// Import barrels for the current user's company
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> ImportBarrelsForCurrentCompany([FromForm] BarrelImportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new BarrelImportError("Please select a CSV file to upload."));
        }

        _logger.LogInformation(
            "Importing barrels for company {CompanyId} by user {UserId} with batch {BatchId} and mash bill {MashBillId}",
            user.CompanyId,
            user.Id,
            request.BatchId,
            request.MashBillId);
        try
        {
            var result = await _barrelsService.ImportBarrelsAsync(user.CompanyId, user.Id, request.File, request.BatchId, request.MashBillId);
            _logger.LogInformation(
                "Successfully imported {Count} barrels for company {CompanyId} in batch {BatchId}. Created new batch: {CreatedNewBatch}",
                result.CreatedCount,
                user.CompanyId,
                result.BatchId,
                result.CreatedNewBatch);
            return Ok(new BarrelImportSummary(result.CreatedCount, result.BatchId, result.CreatedNewBatch));
        }
        catch (BatchRequiredException ex)
        {
            _logger.LogWarning(
                ex,
                "Barrel import requires mash bill for company {CompanyId} by user {UserId}",
                user.CompanyId,
                user.Id);
            return BadRequest(new BarrelImportError(ex.Message, requiresMashBillId: true));
        }
        catch (BarrelImportException ex)
        {
            _logger.LogError(
                ex,
                "Barrel import failed for company {CompanyId} by user {UserId}",
                user.CompanyId,
                user.Id);
            return BadRequest(new BarrelImportError(ex.Message));
        }
    }

    /// <summary>
    /// Import barrels for a specific company (SuperAdmin access)
    /// </summary>
    [HttpPost("company/{companyId}/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> ImportBarrels(int companyId, [FromForm] BarrelImportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new BarrelImportError("Please select a CSV file to upload."));
        }

        _logger.LogInformation(
            "Importing barrels for company {CompanyId} by user {UserId} with batch {BatchId} and mash bill {MashBillId}",
            companyId,
            user.Id,
            request.BatchId,
            request.MashBillId);
        try
        {
            var result = await _barrelsService.ImportBarrelsAsync(companyId, user.Id, request.File, request.BatchId, request.MashBillId);
            _logger.LogInformation(
                "Successfully imported {Count} barrels for company {CompanyId} in batch {BatchId}. Created new batch: {CreatedNewBatch}",
                result.CreatedCount,
                companyId,
                result.BatchId,
                result.CreatedNewBatch);
            return Ok(new BarrelImportSummary(result.CreatedCount, result.BatchId, result.CreatedNewBatch));
        }
        catch (BatchRequiredException ex)
        {
            _logger.LogWarning(
                ex,
                "Barrel import requires mash bill for company {CompanyId} by user {UserId}",
                companyId,
                user.Id);
            return BadRequest(new BarrelImportError(ex.Message, requiresMashBillId: true));
        }
        catch (BarrelImportException ex)
        {
            _logger.LogError(
                ex,
                "Barrel import failed for company {CompanyId} by user {UserId}",
                companyId,
                user.Id);
            return BadRequest(new BarrelImportError(ex.Message));
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        return await GetCurrentUserAsync(_usersService);
    }
}
