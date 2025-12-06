using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Generates TTB Form 5110.28 and Form 5110.40 monthly reports and returns PDF output.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md
/// REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V - Records and Reports
///
/// This controller enforces TTB validation rules, including the official
/// inventory balance equation and proof gallon calculations implemented in
/// the calculator service. Any modification must be reviewed against TTB
/// regulations and the mapping document.
/// </summary>
public sealed class TtbReportsController(
    CaskrDbContext dbContext,
    ITtbReportCalculator ttbReportCalculator,
    ITtbPdfGenerator ttbPdfGenerator,
    ITtbReportWorkflowService workflowService,
    IUsersService usersService,
    ILogger<TtbReportsController> logger) : AuthorizedApiControllerBase
{
    [HttpGet("/api/ttb/reports")]
    [ProducesResponseType(typeof(IEnumerable<TtbReportSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] int companyId,
        [FromQuery] int year,
        [FromQuery] TtbFormType? formType = null,
        [FromQuery] TtbReportStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided for TTB report queries."));
        }

        if (year < 2020)
        {
            return BadRequest(CreateProblem("Year must be 2020 or later for TTB report queries."));
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

        var query = dbContext.TtbMonthlyReports
            .Where(r => r.CompanyId == companyId && r.ReportYear == year);

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        if (formType is not null)
        {
            query = query.Where(r => r.FormType == formType);
        }

        var results = await query
            .OrderByDescending(r => r.ReportMonth)
            .Select(r => new TtbReportSummaryResponse
            {
                Id = r.Id,
                CompanyId = r.CompanyId,
                ReportMonth = r.ReportMonth,
                ReportYear = r.ReportYear,
                FormType = r.FormType,
                Status = r.Status,
                GeneratedAt = r.GeneratedAt,
                ValidationErrors = r.ValidationErrors,
                ValidationWarnings = r.ValidationWarnings
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpGet("/api/ttb/reports/{id:int}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(report.PdfPath) || !System.IO.File.Exists(report.PdfPath))
        {
            return NotFound(CreateProblem("TTB report PDF is unavailable for download."));
        }

        byte[] pdfBytes;
        try
        {
            pdfBytes = await System.IO.File.ReadAllBytesAsync(report.PdfPath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read TTB report PDF for report {ReportId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem("Failed to read TTB report PDF."));
        }

        var fileName = GetFileName(report.FormType, report.ReportMonth, report.ReportYear);
        return File(pdfBytes, "application/pdf", fileName);
    }

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

        if (request.Month is < 1 or > 12)
        {
            return BadRequest(CreateProblem(
                "Month must be between 1 and 12. See docs/TTB_FORM_5110_28_MAPPING.md for reporting periods."));
        }

        if (request.Year < 2020)
        {
            return BadRequest(CreateProblem(
                "Year must be 2020 or later per TTB reporting requirements."));
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
            .Where(r => r.CompanyId == request.CompanyId
                        && r.ReportMonth == request.Month
                        && r.ReportYear == request.Year
                        && r.FormType == request.FormType)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string? existingReportPdfPath = null;
        if (existingReport is not null)
        {
            if (existingReport.Status is TtbReportStatus.Submitted or TtbReportStatus.Approved)
            {
                return Conflict(CreateProblem("Submitted or approved reports cannot be regenerated."));
            }

            existingReportPdfPath = existingReport.PdfPath;
            dbContext.TtbMonthlyReports.Remove(existingReport);
        }

        TtbMonthlyReportData reportData;
        TtbForm5110_40Data? storageReportData = null;
        try
        {
            if (request.FormType == TtbFormType.Form5110_40)
            {
                storageReportData = await ttbReportCalculator.CalculateForm5110_40Async(
                    request.CompanyId,
                    request.Month,
                    request.Year,
                    cancellationToken);

                // Storage report uses barrel counts aggregated from inventory balance equation
                // See docs/TTB_FORM_5110_28_MAPPING.md (Calculation Formulas)
                reportData = new TtbMonthlyReportData
                {
                    CompanyId = request.CompanyId,
                    Month = request.Month,
                    Year = request.Year,
                    StartDate = new DateTime(request.Year, request.Month, 1),
                    EndDate = new DateTime(request.Year, request.Month, 1).AddMonths(1).AddDays(-1)
                };
            }
            else
            {
                // TTB Formula: Proof Gallons = Wine Gallons × (ABV × 2) / 100
                // See docs/TTB_FORM_5110_28_MAPPING.md (Calculation Formulas)
                reportData = await ttbReportCalculator.CalculateMonthlyReportAsync(
                    request.CompanyId,
                    request.Month,
                    request.Year,
                    cancellationToken);
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Validation failed while calculating TTB monthly report");
            return BadRequest(CreateProblem(ex.Message));
        }

        if (request.FormType == TtbFormType.Form5110_40)
        {
            if (!HasStorageReportContent(storageReportData))
            {
                logger.LogWarning(
                    "No TTB storage data found for company {CompanyId} month {Month} year {Year} when generating report.",
                    request.CompanyId,
                    request.Month,
                    request.Year);
                return BadRequest(CreateProblem("No TTB storage data found for the specified month."));
            }
        }
        else if (!HasReportContent(reportData))
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
            pdfResult = request.FormType == TtbFormType.Form5110_40
                ? await ttbPdfGenerator.GenerateForm5110_40Async(storageReportData!, cancellationToken)
                : await ttbPdfGenerator.GenerateForm5110_28Async(reportData, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(
                ex,
                "TTB form template not found for company {CompanyId} month {Month} year {Year}",
                request.CompanyId,
                request.Month,
                request.Year);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem("TTB form template not found. Please contact support."));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(
                ex,
                "Invalid operation during TTB report generation for company {CompanyId} month {Month} year {Year}: {Message}",
                request.CompanyId,
                request.Month,
                request.Year,
                ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem($"Failed to generate TTB report: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to generate TTB report PDF for company {CompanyId} month {Month} year {Year}",
                request.CompanyId,
                request.Month,
                request.Year);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblem("Failed to generate TTB report PDF. Please try again or contact support if the problem persists."));
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

        // Serialize validation results for storage
        var validationErrors = reportData.Validation.Errors.Count > 0
            ? JsonSerializer.Serialize(reportData.Validation.Errors)
            : null;
        var validationWarnings = reportData.Validation.Warnings.Count > 0
            ? JsonSerializer.Serialize(reportData.Validation.Warnings)
            : null;

        // Determine status based on validation results
        var status = reportData.Validation.IsValid
            ? TtbReportStatus.Draft
            : TtbReportStatus.ValidationFailed;

        var report = new TtbMonthlyReport
        {
            CompanyId = request.CompanyId,
            ReportMonth = request.Month,
            ReportYear = request.Year,
            Status = status,
            FormType = request.FormType,
            GeneratedAt = DateTime.UtcNow,
            PdfPath = pdfResult.FilePath,
            ValidationErrors = validationErrors,
            ValidationWarnings = validationWarnings,
            CreatedByUserId = user.Id
        };

        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(existingReportPdfPath)
            && !string.Equals(existingReportPdfPath, pdfResult.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            TryDeletePdf(existingReportPdfPath);
        }

        var fileName = GetFileName(request.FormType, request.Month, request.Year);
        return File(pdfResult.Content, "application/pdf", fileName);
    }

    /// <summary>
    /// Submits a report for review (Draft → PendingReview).
    /// </summary>
    [HttpPost("/api/ttb/reports/{id:int}/submit-for-review")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitForReview(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        var result = await workflowService.SubmitForReviewAsync(id, user.Id, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(CreateProblem(result.ErrorMessage ?? "Failed to submit report for review."));
        }

        return Ok(MapToDetailResponse(result.Report!));
    }

    /// <summary>
    /// Approves a report for TTB submission (PendingReview → Approved).
    /// Requires ComplianceManager role or higher.
    /// </summary>
    [HttpPost("/api/ttb/reports/{id:int}/approve")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] TtbReportReviewRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        var result = await workflowService.ApproveReportAsync(id, user.Id, request?.ReviewNotes, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(CreateProblem(result.ErrorMessage ?? "Failed to approve report."));
        }

        return Ok(MapToDetailResponse(result.Report!));
    }

    /// <summary>
    /// Rejects a report back to draft status (PendingReview → Draft).
    /// Requires ComplianceManager role or higher.
    /// </summary>
    [HttpPost("/api/ttb/reports/{id:int}/reject")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] TtbReportReviewRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        var result = await workflowService.RejectReportAsync(id, user.Id, request?.ReviewNotes, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(CreateProblem(result.ErrorMessage ?? "Failed to reject report."));
        }

        return Ok(MapToDetailResponse(result.Report!));
    }

    /// <summary>
    /// Marks a report as submitted to TTB (Approved → Submitted).
    /// Records the TTB confirmation number and locks all related data.
    /// </summary>
    [HttpPost("/api/ttb/reports/{id:int}/submit-to-ttb")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitToTtb(
        int id,
        [FromBody] TtbSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        var result = await workflowService.SubmitToTtbAsync(id, user.Id, request.ConfirmationNumber, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(CreateProblem(result.ErrorMessage ?? "Failed to submit report to TTB."));
        }

        return Ok(MapToDetailResponse(result.Report!));
    }

    /// <summary>
    /// Archives a submitted report (Submitted → Archived).
    /// </summary>
    [HttpPost("/api/ttb/reports/{id:int}/archive")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        var result = await workflowService.ArchiveReportAsync(id, user.Id, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(CreateProblem(result.ErrorMessage ?? "Failed to archive report."));
        }

        return Ok(MapToDetailResponse(result.Report!));
    }

    /// <summary>
    /// Gets the detailed status of a report including workflow information.
    /// </summary>
    [HttpGet("/api/ttb/reports/{id:int}")]
    [ProducesResponseType(typeof(TtbReportDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var report = await dbContext.TtbMonthlyReports
            .Include(r => r.CreatedByUser)
            .Include(r => r.SubmittedForReviewByUser)
            .Include(r => r.ReviewedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return NotFound(CreateProblem("TTB report was not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != report.CompanyId)
        {
            return Forbid();
        }

        return Ok(MapToDetailResponse(report));
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await GetCurrentUserAsync(usersService);
    }

    private static TtbReportDetailResponse MapToDetailResponse(TtbMonthlyReport report)
    {
        return new TtbReportDetailResponse
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            ReportMonth = report.ReportMonth,
            ReportYear = report.ReportYear,
            FormType = report.FormType,
            Status = report.Status,
            GeneratedAt = report.GeneratedAt,
            SubmittedAt = report.SubmittedAt,
            TtbConfirmationNumber = report.TtbConfirmationNumber,
            ValidationErrors = report.ValidationErrors,
            ValidationWarnings = report.ValidationWarnings,
            CreatedByUserId = report.CreatedByUserId,
            CreatedByUserName = report.CreatedByUser?.Name,
            SubmittedForReviewByUserId = report.SubmittedForReviewByUserId,
            SubmittedForReviewByUserName = report.SubmittedForReviewByUser?.Name,
            SubmittedForReviewAt = report.SubmittedForReviewAt,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedByUserName = report.ReviewedByUser?.Name,
            ReviewedAt = report.ReviewedAt,
            ApprovedByUserId = report.ApprovedByUserId,
            ApprovedByUserName = report.ApprovedByUser?.Name,
            ApprovedAt = report.ApprovedAt,
            ReviewNotes = report.ReviewNotes
        };
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

    private static bool HasStorageReportContent(TtbForm5110_40Data? reportData)
    {
        if (reportData is null)
        {
            return false;
        }

        return reportData.OpeningBarrels > 0
               || reportData.BarrelsReceived > 0
               || reportData.BarrelsRemoved > 0
               || reportData.ClosingBarrels > 0
               || (reportData.ProofGallonsByWarehouse?.Any() == true);
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

    private static string GetFileName(TtbFormType formType, int month, int year)
    {
        var formNumber = formType == TtbFormType.Form5110_40 ? "5110_40" : "5110_28";
        return $"Form_{formNumber}_{month:D2}_{year}.pdf";
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

    /// <summary>
    /// TTB form type (5110.28 or 5110.40).
    /// </summary>
    public TtbFormType FormType { get; set; } = TtbFormType.Form5110_28;
}

public sealed class TtbReportSummaryResponse
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int ReportMonth { get; set; }

    public int ReportYear { get; set; }

    public TtbFormType FormType { get; set; }

    public TtbReportStatus Status { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public string? ValidationErrors { get; set; }

    public string? ValidationWarnings { get; set; }
}

/// <summary>
/// Detailed response including workflow tracking information.
/// </summary>
public sealed class TtbReportDetailResponse
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int ReportMonth { get; set; }

    public int ReportYear { get; set; }

    public TtbFormType FormType { get; set; }

    public TtbReportStatus Status { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? TtbConfirmationNumber { get; set; }

    public string? ValidationErrors { get; set; }

    public string? ValidationWarnings { get; set; }

    public int CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    public int? SubmittedForReviewByUserId { get; set; }

    public string? SubmittedForReviewByUserName { get; set; }

    public DateTime? SubmittedForReviewAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public string? ReviewedByUserName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? ApprovedByUserId { get; set; }

    public string? ApprovedByUserName { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Request payload for reviewing a report.
/// </summary>
public sealed class TtbReportReviewRequest
{
    /// <summary>
    /// Optional notes or feedback from the reviewer.
    /// </summary>
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Request payload for submitting a report to TTB.
/// </summary>
public sealed class TtbSubmissionRequest
{
    /// <summary>
    /// The confirmation number received from TTB after submission.
    /// </summary>
    [Required]
    public string ConfirmationNumber { get; set; } = null!;
}
