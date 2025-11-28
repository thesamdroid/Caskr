using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// API endpoints for generating TTB monthly reports (Form 5110.28).
/// </summary>
public sealed class TtbReportsController(
    CaskrDbContext dbContext,
    ITtbReportCalculator ttbReportCalculator,
    ITtbPdfGenerator ttbPdfGenerator,
    IUsersService usersService,
    ILogger<TtbReportsController> logger) : AuthorizedApiControllerBase
{
    /// <summary>
    /// Generates a draft TTB monthly report (Form 5110.28) for the specified company and period.
    /// </summary>
    /// <param name="request">The report generation request containing company, month, and year.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A PDF file for download when generation succeeds.</returns>
    [HttpPost("/api/ttb/reports/generate")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Generate(
        [FromBody] TtbReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Year < 2020)
        {
            return BadRequest(CreateProblem("Year must be 2020 or later."));
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

        var existingReport = await dbContext.TtbMonthlyReports
            .Where(r => r.CompanyId == request.CompanyId && r.ReportMonth == request.Month && r.ReportYear == request.Year)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingReport is not null)
        {
            if (existingReport.Status is TtbReportStatus.Submitted or TtbReportStatus.Approved)
            {
                return Conflict(CreateProblem("Submitted or approved reports cannot be regenerated."));
            }

            dbContext.TtbMonthlyReports.Remove(existingReport);
            TryDeletePdf(existingReport.PdfPath);
        }

        TtbMonthlyReportData reportData;
        try
        {
            reportData = await ttbReportCalculator.CalculateMonthlyReportAsync(
                request.CompanyId,
                request.Month,
                request.Year,
                cancellationToken);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Validation failed while calculating TTB monthly report");
            return BadRequest(CreateProblem(ex.Message));
        }

        if (!HasReportContent(reportData))
        {
            logger.LogWarning(
                "No TTB data found for company {CompanyId} month {Month} year {Year} when generating report.",
                request.CompanyId,
                request.Month,
                request.Year);
            return BadRequest(CreateProblem("No TTB data found for the specified month."));
        }

        PdfGenerationResult pdfResult;
        try
        {
            pdfResult = await ttbPdfGenerator.GenerateForm5110_28Async(reportData, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to generate TTB report PDF for company {CompanyId} month {Month} year {Year}",
                request.CompanyId,
                request.Month,
                request.Year);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem("Failed to generate TTB report PDF."));
        }

        if (pdfResult.Content.Length == 0)
        {
            logger.LogError(
                "Generated PDF was empty for company {CompanyId} month {Month} year {Year}",
                request.CompanyId,
                request.Month,
                request.Year);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem("Generated PDF is empty."));
        }

        var report = new TtbMonthlyReport
        {
            CompanyId = request.CompanyId,
            ReportMonth = request.Month,
            ReportYear = request.Year,
            Status = TtbReportStatus.Draft,
            GeneratedAt = DateTime.UtcNow,
            PdfPath = pdfResult.FilePath,
            CreatedByUserId = user.Id
        };

        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        var fileName = $"Form_5110_28_{request.Month:D2}_{request.Year}.pdf";
        return File(pdfResult.Content, "application/pdf", fileName);
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId)
            ? await usersService.GetUserByIdAsync(userId)
            : null;
    }

    private static bool HasReportContent(TtbMonthlyReportData reportData)
    {
        return reportData.OpeningInventory.Rows.Any()
               || reportData.Production.Rows.Any()
               || reportData.Transfers.TransfersIn.Any()
               || reportData.Transfers.TransfersOut.Any()
               || reportData.Losses.Rows.Any()
               || reportData.ClosingInventory.Rows.Any();
    }

    private static ProblemDetails CreateProblem(string detail) => new()
    {
        Detail = detail,
        Title = "TTB report generation failed"
    };

    private void TryDeletePdf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete existing TTB report PDF at {Path}", path);
        }
    }
}

/// <summary>
/// Request payload for generating a TTB monthly report.
/// </summary>
public sealed class TtbReportGenerationRequest
{
    /// <summary>
    /// Company identifier the report is generated for.
    /// </summary>
    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Month of the report (1-12).
    /// </summary>
    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>
    /// Calendar year of the report.
    /// </summary>
    [Range(2020, int.MaxValue)]
    public int Year { get; set; }
}
