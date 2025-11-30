using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Manages federal excise tax calculations and reporting for distilled spirits
/// </summary>
public sealed class TtbExciseTaxController(
    ITtbExciseTaxService exciseTaxService,
    CaskrDbContext dbContext,
    IUsersService usersService,
    ILogger<TtbExciseTaxController> logger) : AuthorizedApiControllerBase
{
    /// <summary>
    /// Calculate federal excise tax for an order
    /// </summary>
    [HttpPost("/api/ttb/excise-tax/calculate")]
    [ProducesResponseType(typeof(ExciseTaxCalculation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateTax(
        [FromBody] ExciseTaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId <= 0)
        {
            return BadRequest(CreateProblem("OrderId must be provided."));
        }

        if (request.CompanyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != request.CompanyId)
        {
            return Forbid();
        }

        // Verify order exists and belongs to company
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return NotFound(CreateProblem($"Order {request.OrderId} not found."));
        }

        if (order.CompanyId != request.CompanyId)
        {
            return BadRequest(CreateProblem("Order does not belong to the specified company."));
        }

        try
        {
            var calculation = await exciseTaxService.CalculateTaxAsync(request.OrderId);
            logger.LogInformation(
                "User {UserId} calculated excise tax for order {OrderId}: ${TaxAmount:F2}",
                user.Id,
                request.OrderId,
                calculation.TotalTaxDue);
            return Ok(calculation);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to calculate excise tax for order {OrderId}", request.OrderId);
            return BadRequest(CreateProblem(ex.Message));
        }
    }

    /// <summary>
    /// Record a tax determination for an order
    /// </summary>
    [HttpPost("/api/ttb/excise-tax/record")]
    [ProducesResponseType(typeof(TtbTaxDetermination), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordTaxDetermination(
        [FromBody] ExciseTaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId <= 0)
        {
            return BadRequest(CreateProblem("OrderId must be provided."));
        }

        if (request.CompanyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != request.CompanyId)
        {
            return Forbid();
        }

        // Verify order exists and belongs to company
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return NotFound(CreateProblem($"Order {request.OrderId} not found."));
        }

        if (order.CompanyId != request.CompanyId)
        {
            return BadRequest(CreateProblem("Order does not belong to the specified company."));
        }

        try
        {
            // Calculate tax first
            var calculation = await exciseTaxService.CalculateTaxAsync(request.OrderId);

            // Record tax determination
            var taxDetermination = await exciseTaxService.RecordTaxDeterminationAsync(request.OrderId, calculation);

            logger.LogInformation(
                "User {UserId} recorded excise tax determination {TaxDeterminationId} for order {OrderId}: ${TaxAmount:F2}",
                user.Id,
                taxDetermination.Id,
                request.OrderId,
                taxDetermination.TaxAmount);

            // Attempt to post to QuickBooks if integration is enabled
            var posted = await exciseTaxService.PostTaxLiabilityToQuickBooksAsync(taxDetermination.Id);
            if (posted)
            {
                logger.LogInformation(
                    "Tax determination {TaxDeterminationId} posted to QuickBooks",
                    taxDetermination.Id);
            }

            return CreatedAtAction(nameof(GetTaxDetermination), new { id = taxDetermination.Id }, taxDetermination);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to record tax determination for order {OrderId}", request.OrderId);
            return BadRequest(CreateProblem(ex.Message));
        }
    }

    /// <summary>
    /// Get a specific tax determination by ID
    /// </summary>
    [HttpGet("/api/ttb/excise-tax/determinations/{id:int}")]
    [ProducesResponseType(typeof(TtbTaxDetermination), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaxDetermination(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var taxDetermination = await dbContext.TtbTaxDeterminations
            .Include(td => td.Order)
            .Include(td => td.Company)
            .FirstOrDefaultAsync(td => td.Id == id, cancellationToken);

        if (taxDetermination is null)
        {
            return NotFound(CreateProblem("Tax determination not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != taxDetermination.CompanyId)
        {
            return Forbid();
        }

        return Ok(taxDetermination);
    }

    /// <summary>
    /// List tax determinations for a company
    /// </summary>
    [HttpGet("/api/ttb/excise-tax/determinations")]
    [ProducesResponseType(typeof(IEnumerable<TtbTaxDetermination>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListTaxDeterminations(
        [FromQuery, Required] int companyId,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != companyId)
        {
            return Forbid();
        }

        var query = dbContext.TtbTaxDeterminations
            .Include(td => td.Order)
            .Where(td => td.CompanyId == companyId);

        if (month.HasValue && year.HasValue)
        {
            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(td => td.DeterminationDate >= startDate && td.DeterminationDate <= endDate);
        }

        var taxDeterminations = await query
            .OrderByDescending(td => td.DeterminationDate)
            .ThenByDescending(td => td.Id)
            .ToListAsync(cancellationToken);

        return Ok(taxDeterminations);
    }

    /// <summary>
    /// Post tax liability to QuickBooks for a specific tax determination
    /// </summary>
    [HttpPost("/api/ttb/excise-tax/determinations/{id:int}/post-to-quickbooks")]
    [ProducesResponseType(typeof(PostToQuickBooksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostToQuickBooks(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var taxDetermination = await dbContext.TtbTaxDeterminations
            .FirstOrDefaultAsync(td => td.Id == id, cancellationToken);

        if (taxDetermination is null)
        {
            return NotFound(CreateProblem("Tax determination not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != taxDetermination.CompanyId)
        {
            return Forbid();
        }

        try
        {
            var posted = await exciseTaxService.PostTaxLiabilityToQuickBooksAsync(id);

            if (posted)
            {
                logger.LogInformation(
                    "User {UserId} posted tax determination {TaxDeterminationId} to QuickBooks",
                    user.Id,
                    id);
                return Ok(new PostToQuickBooksResponse { Success = true, Message = "Tax liability posted to QuickBooks successfully." });
            }
            else
            {
                return Ok(new PostToQuickBooksResponse { Success = false, Message = "QuickBooks integration not enabled or posting failed." });
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to post tax determination {TaxDeterminationId} to QuickBooks", id);
            return BadRequest(CreateProblem(ex.Message));
        }
    }

    /// <summary>
    /// Generate excise tax report for a month
    /// </summary>
    [HttpGet("/api/ttb/excise-tax/report")]
    [ProducesResponseType(typeof(ExciseTaxReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateReport(
        [FromQuery, Required] int companyId,
        [FromQuery, Required] int month,
        [FromQuery, Required] int year,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        if (month < 1 || month > 12)
        {
            return BadRequest(CreateProblem("Month must be between 1 and 12."));
        }

        if (year < 2020)
        {
            return BadRequest(CreateProblem("Year must be 2020 or later."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != companyId)
        {
            return Forbid();
        }

        try
        {
            var report = await exciseTaxService.GenerateExciseTaxReportAsync(companyId, month, year);

            logger.LogInformation(
                "User {UserId} generated excise tax report for company {CompanyId}, {Month}/{Year}",
                user.Id,
                companyId,
                month,
                year);

            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to generate excise tax report for company {CompanyId}, {Month}/{Year}", companyId, month, year);
            return BadRequest(CreateProblem(ex.Message));
        }
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await usersService.GetUserByIdAsync(userId);
    }

    private static ProblemDetails CreateProblem(string detail) => new()
    {
        Detail = detail,
        Title = "Excise tax operation failed"
    };
}

/// <summary>
/// Response for QuickBooks posting operation
/// </summary>
public class PostToQuickBooksResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
