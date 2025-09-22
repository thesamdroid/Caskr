using System.Security.Claims;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

public class BarrelImportRequest
{
    public IFormFile? File { get; set; }

    public int? BatchId { get; set; }

    public int? MashBillId { get; set; }
}

public record BarrelImportError(string message, bool? requiresMashBillId = null);

public record BarrelImportSummary(int created, int batchId, bool createdNewBatch);
public class BarrelsController(IBarrelsService barrelsService, IUsersService usersService) : AuthorizedApiControllerBase
{
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<Barrel>>> GetBarrelsForCompany(int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var barrels = await barrelsService.GetBarrelsForCompanyAsync(companyId);
        return Ok(barrels.ToList());
    }

    [HttpGet("company/{companyId}/forecast")]
    public async Task<ActionResult<object>> Forecast(int companyId, [FromQuery] DateTime targetDate, [FromQuery] int ageYears)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var barrels = await barrelsService.ForecastBarrelsAsync(companyId, targetDate, ageYears);
        var list = barrels.ToList();
        return Ok(new { barrels = list, count = list.Count });
    }

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

        try
        {
            var result = await barrelsService.ImportBarrelsAsync(companyId, user.Id, request.File, request.BatchId, request.MashBillId);
            return Ok(new BarrelImportSummary(result.CreatedCount, result.BatchId, result.CreatedNewBatch));
        }
        catch (BatchRequiredException ex)
        {
            return BadRequest(new BarrelImportError(ex.Message, requiresMashBillId: true));
        }
        catch (BarrelImportException ex)
        {
            return BadRequest(new BarrelImportError(ex.Message));
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await usersService.GetUserByIdAsync(userId);
    }
}
