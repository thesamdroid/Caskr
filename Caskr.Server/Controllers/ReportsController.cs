using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Request model for executing a report.
/// </summary>
public class ExecuteReportRequest
{
    public int ReportTemplateId { get; set; }
    public Dictionary<string, object?>? Filters { get; set; }
    public int Page { get; set; } = 1;
    public int? PageSize { get; set; }
    public List<ReportSortSpec>? SortOverride { get; set; }
}

/// <summary>
/// Request model for saving a report configuration.
/// </summary>
public class SaveReportRequest
{
    public int ReportTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Dictionary<string, object?>? FilterValues { get; set; }
    public bool IsFavorite { get; set; }
}

/// <summary>
/// Request model for updating a saved report.
/// </summary>
public class UpdateSavedReportRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object?>? FilterValues { get; set; }
    public bool? IsFavorite { get; set; }
}

/// <summary>
/// DTO for report template list items.
/// </summary>
public class ReportTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public bool IsSystemTemplate { get; set; }
    public string? FilterDefinition { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for saved report list items.
/// </summary>
public class SavedReportDto
{
    public int Id { get; set; }
    public int ReportTemplateId { get; set; }
    public string ReportTemplateName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? FilterValues { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastRunAt { get; set; }
    public int RunCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Controller for report builder functionality including executing reports,
/// managing saved reports, and exporting report data.
/// </summary>
public class ReportsController(
    CaskrDbContext dbContext,
    IReportingService reportingService,
    IUsersService usersService,
    ILogger<ReportsController> logger)
    : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext = dbContext;
    private readonly IReportingService _reportingService = reportingService;
    private readonly IUsersService _usersService = usersService;
    private readonly ILogger<ReportsController> _logger = logger;

    /// <summary>
    /// Gets all available report templates for a company.
    /// </summary>
    [HttpGet("templates/company/{companyId}")]
    public async Task<ActionResult<IEnumerable<ReportTemplateDto>>> GetTemplates(int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Fetching report templates for company {CompanyId}", companyId);

        var templates = await _dbContext.ReportTemplates
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new ReportTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = InferCategory(t.Name, t.Description),
                IsSystemTemplate = t.IsSystemTemplate,
                FilterDefinition = t.Filters,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Gets a specific report template by ID.
    /// </summary>
    [HttpGet("templates/{templateId}/company/{companyId}")]
    public async Task<ActionResult<ReportTemplateDto>> GetTemplate(int templateId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var template = await _dbContext.ReportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId && t.IsActive);

        if (template == null)
        {
            return NotFound();
        }

        return Ok(new ReportTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = InferCategory(template.Name, template.Description),
            IsSystemTemplate = template.IsSystemTemplate,
            FilterDefinition = template.Filters,
            CreatedAt = template.CreatedAt
        });
    }

    /// <summary>
    /// Executes a report with the specified filters.
    /// </summary>
    [HttpPost("execute/company/{companyId}")]
    public async Task<ActionResult<ReportResult>> ExecuteReport(int companyId, [FromBody] ExecuteReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Executing report template {TemplateId} for company {CompanyId} by user {UserId}",
            request.ReportTemplateId,
            companyId,
            user.Id);

        try
        {
            var executionParams = new ReportExecutionParameters
            {
                Parameters = request.Filters ?? new Dictionary<string, object?>(),
                Page = request.Page,
                PageSize = request.PageSize,
                SortOverride = request.SortOverride
            };

            var result = await _reportingService.ExecuteReportAsync(
                request.ReportTemplateId,
                companyId,
                executionParams);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report execution failed for template {TemplateId}", request.ReportTemplateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all saved reports for the current user.
    /// </summary>
    [HttpGet("saved/company/{companyId}")]
    public async Task<ActionResult<IEnumerable<SavedReportDto>>> GetSavedReports(int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Fetching saved reports for user {UserId} in company {CompanyId}", user.Id, companyId);

        var savedReports = await _dbContext.SavedReports
            .AsNoTracking()
            .Include(sr => sr.ReportTemplate)
            .Where(sr => sr.CompanyId == companyId && sr.UserId == user.Id)
            .OrderByDescending(sr => sr.IsFavorite)
            .ThenByDescending(sr => sr.LastRunAt)
            .Select(sr => new SavedReportDto
            {
                Id = sr.Id,
                ReportTemplateId = sr.ReportTemplateId,
                ReportTemplateName = sr.ReportTemplate.Name,
                Name = sr.Name,
                Description = sr.Description,
                FilterValues = sr.FilterValues,
                IsFavorite = sr.IsFavorite,
                LastRunAt = sr.LastRunAt,
                RunCount = sr.RunCount,
                CreatedAt = sr.CreatedAt
            })
            .ToListAsync();

        return Ok(savedReports);
    }

    /// <summary>
    /// Creates a new saved report.
    /// </summary>
    [HttpPost("saved/company/{companyId}")]
    public async Task<ActionResult<SavedReportDto>> CreateSavedReport(int companyId, [FromBody] SaveReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        // Verify the template exists and belongs to the company
        var templateExists = await _dbContext.ReportTemplates
            .AnyAsync(t => t.Id == request.ReportTemplateId && t.CompanyId == companyId && t.IsActive);

        if (!templateExists)
        {
            return BadRequest(new { error = "Report template not found" });
        }

        _logger.LogInformation(
            "Creating saved report '{Name}' for template {TemplateId} by user {UserId}",
            request.Name,
            request.ReportTemplateId,
            user.Id);

        var savedReport = new SavedReport
        {
            CompanyId = companyId,
            UserId = user.Id,
            ReportTemplateId = request.ReportTemplateId,
            Name = request.Name,
            Description = request.Description,
            FilterValues = request.FilterValues != null
                ? JsonSerializer.Serialize(request.FilterValues)
                : null,
            IsFavorite = request.IsFavorite,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.SavedReports.Add(savedReport);
        await _dbContext.SaveChangesAsync();

        // Load the template name for the response
        var template = await _dbContext.ReportTemplates.FindAsync(request.ReportTemplateId);

        return CreatedAtAction(nameof(GetSavedReports), new { companyId }, new SavedReportDto
        {
            Id = savedReport.Id,
            ReportTemplateId = savedReport.ReportTemplateId,
            ReportTemplateName = template?.Name ?? "Unknown",
            Name = savedReport.Name,
            Description = savedReport.Description,
            FilterValues = savedReport.FilterValues,
            IsFavorite = savedReport.IsFavorite,
            LastRunAt = savedReport.LastRunAt,
            RunCount = savedReport.RunCount,
            CreatedAt = savedReport.CreatedAt
        });
    }

    /// <summary>
    /// Updates a saved report.
    /// </summary>
    [HttpPut("saved/{savedReportId}/company/{companyId}")]
    public async Task<ActionResult<SavedReportDto>> UpdateSavedReport(
        int savedReportId,
        int companyId,
        [FromBody] UpdateSavedReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var savedReport = await _dbContext.SavedReports
            .Include(sr => sr.ReportTemplate)
            .FirstOrDefaultAsync(sr => sr.Id == savedReportId && sr.CompanyId == companyId && sr.UserId == user.Id);

        if (savedReport == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Updating saved report {SavedReportId} by user {UserId}", savedReportId, user.Id);

        if (request.Name != null)
            savedReport.Name = request.Name;
        if (request.Description != null)
            savedReport.Description = request.Description;
        if (request.FilterValues != null)
            savedReport.FilterValues = JsonSerializer.Serialize(request.FilterValues);
        if (request.IsFavorite.HasValue)
            savedReport.IsFavorite = request.IsFavorite.Value;

        savedReport.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new SavedReportDto
        {
            Id = savedReport.Id,
            ReportTemplateId = savedReport.ReportTemplateId,
            ReportTemplateName = savedReport.ReportTemplate.Name,
            Name = savedReport.Name,
            Description = savedReport.Description,
            FilterValues = savedReport.FilterValues,
            IsFavorite = savedReport.IsFavorite,
            LastRunAt = savedReport.LastRunAt,
            RunCount = savedReport.RunCount,
            CreatedAt = savedReport.CreatedAt
        });
    }

    /// <summary>
    /// Deletes a saved report.
    /// </summary>
    [HttpDelete("saved/{savedReportId}/company/{companyId}")]
    public async Task<ActionResult> DeleteSavedReport(int savedReportId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var savedReport = await _dbContext.SavedReports
            .FirstOrDefaultAsync(sr => sr.Id == savedReportId && sr.CompanyId == companyId && sr.UserId == user.Id);

        if (savedReport == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Deleting saved report {SavedReportId} by user {UserId}", savedReportId, user.Id);

        _dbContext.SavedReports.Remove(savedReport);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Runs a saved report and updates its run statistics.
    /// </summary>
    [HttpPost("saved/{savedReportId}/run/company/{companyId}")]
    public async Task<ActionResult<ReportResult>> RunSavedReport(int savedReportId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var savedReport = await _dbContext.SavedReports
            .FirstOrDefaultAsync(sr => sr.Id == savedReportId && sr.CompanyId == companyId && sr.UserId == user.Id);

        if (savedReport == null)
        {
            return NotFound();
        }

        _logger.LogInformation(
            "Running saved report {SavedReportId} (template {TemplateId}) for user {UserId}",
            savedReportId,
            savedReport.ReportTemplateId,
            user.Id);

        // Parse saved filter values
        var filters = string.IsNullOrEmpty(savedReport.FilterValues)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(savedReport.FilterValues) ?? new Dictionary<string, object?>();

        try
        {
            var result = await _reportingService.ExecuteReportAsync(
                savedReport.ReportTemplateId,
                companyId,
                new ReportExecutionParameters { Parameters = filters });

            // Update run statistics
            savedReport.LastRunAt = DateTime.UtcNow;
            savedReport.RunCount++;
            await _dbContext.SaveChangesAsync();

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Saved report execution failed for {SavedReportId}", savedReportId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports report results to CSV format.
    /// </summary>
    [HttpPost("export/csv/company/{companyId}")]
    public async Task<ActionResult> ExportToCsv(int companyId, [FromBody] ExecuteReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Exporting report {TemplateId} to CSV for company {CompanyId}",
            request.ReportTemplateId,
            companyId);

        try
        {
            // Execute report without pagination to get all data
            var executionParams = new ReportExecutionParameters
            {
                Parameters = request.Filters ?? new Dictionary<string, object?>(),
                Page = 1,
                PageSize = 10000 // Get all rows for export
            };

            var result = await _reportingService.ExecuteReportAsync(
                request.ReportTemplateId,
                companyId,
                executionParams);

            // Build CSV content
            var csv = new StringBuilder();

            // Header row
            csv.AppendLine(string.Join(",", result.Columns.Select(c => EscapeCsvField(c.DisplayName))));

            // Data rows
            foreach (var row in result.Rows)
            {
                var values = result.Columns.Select(c =>
                {
                    row.TryGetValue(c.Name, out var value);
                    return EscapeCsvField(FormatValue(value, c.DataType));
                });
                csv.AppendLine(string.Join(",", values));
            }

            var fileName = $"{result.TemplateName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CSV export failed for template {TemplateId}", request.ReportTemplateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports report results to JSON format (for Excel generation on client).
    /// </summary>
    [HttpPost("export/json/company/{companyId}")]
    public async Task<ActionResult> ExportToJson(int companyId, [FromBody] ExecuteReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Exporting report {TemplateId} to JSON for company {CompanyId}",
            request.ReportTemplateId,
            companyId);

        try
        {
            var executionParams = new ReportExecutionParameters
            {
                Parameters = request.Filters ?? new Dictionary<string, object?>(),
                Page = 1,
                PageSize = 10000
            };

            var result = await _reportingService.ExecuteReportAsync(
                request.ReportTemplateId,
                companyId,
                executionParams);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "JSON export failed for template {TemplateId}", request.ReportTemplateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _usersService.GetUserByIdAsync(userId);
    }

    private static string InferCategory(string name, string? description)
    {
        var text = $"{name} {description}".ToLowerInvariant();

        if (text.Contains("financial") || text.Contains("revenue") || text.Contains("cost") ||
            text.Contains("profit") || text.Contains("valuation") || text.Contains("tax"))
            return "Financial";

        if (text.Contains("inventory") || text.Contains("barrel") || text.Contains("stock") ||
            text.Contains("warehouse") || text.Contains("aging"))
            return "Inventory";

        if (text.Contains("production") || text.Contains("batch") || text.Contains("yield") ||
            text.Contains("efficiency") || text.Contains("mash"))
            return "Production";

        if (text.Contains("compliance") || text.Contains("ttb") || text.Contains("audit") ||
            text.Contains("transfer") || text.Contains("gauge"))
            return "Compliance";

        return "General";
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static string FormatValue(object? value, string dataType)
    {
        if (value == null)
            return "";

        return dataType switch
        {
            "datetime" when value is DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            "date" when value is DateTime dt => dt.ToString("yyyy-MM-dd"),
            "decimal" or "number" => value.ToString() ?? "",
            "boolean" when value is bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? ""
        };
    }
}
