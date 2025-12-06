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
/// Request model for creating a custom report.
/// </summary>
public class CreateCustomReportRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = "Custom Reports";
    public List<string> DataSources { get; set; } = new();
    public List<CustomReportColumn> SelectedColumns { get; set; } = new();
    public List<CustomReportFilter> Filters { get; set; } = new();
    public List<CustomReportGroupBy> GroupBy { get; set; } = new();
    public List<CustomReportOrderBy> OrderBy { get; set; } = new();
}

/// <summary>
/// Column specification for custom report.
/// </summary>
public class CustomReportColumn
{
    public string ColumnName { get; set; } = null!;
    public string SourceTable { get; set; } = null!;
    public string Alias { get; set; } = null!;
    public string? Aggregation { get; set; }
    public CustomReportColumnFormatting? Formatting { get; set; }
}

/// <summary>
/// Column formatting options.
/// </summary>
public class CustomReportColumnFormatting
{
    public string? DateFormat { get; set; }
    public int? DecimalPlaces { get; set; }
    public bool? IsCurrency { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
}

/// <summary>
/// Filter specification for custom report.
/// </summary>
public class CustomReportFilter
{
    public string ColumnName { get; set; } = null!;
    public string SourceTable { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public object? Value { get; set; }
    public string Logic { get; set; } = "AND";
}

/// <summary>
/// Group by specification for custom report.
/// </summary>
public class CustomReportGroupBy
{
    public string ColumnName { get; set; } = null!;
    public string SourceTable { get; set; } = null!;
}

/// <summary>
/// Order by specification for custom report.
/// </summary>
public class CustomReportOrderBy
{
    public string ColumnName { get; set; } = null!;
    public string SourceTable { get; set; } = null!;
    public string Direction { get; set; } = "ASC";
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
                ReportTemplateName = sr.ReportTemplate != null ? sr.ReportTemplate.Name : "Unknown",
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
            ReportTemplateName = savedReport.ReportTemplate?.Name ?? "Unknown",
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
        return await GetCurrentUserAsync(_usersService);
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

    /// <summary>
    /// Previews a custom report configuration without saving it.
    /// </summary>
    [HttpPost("custom/preview/company/{companyId}")]
    public async Task<ActionResult<CustomReportPreviewResult>> PreviewCustomReport(
        int companyId,
        [FromBody] CreateCustomReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Previewing custom report for company {CompanyId} by user {UserId}",
            companyId,
            user.Id);

        try
        {
            // Validate the request
            if (request.DataSources.Count == 0)
            {
                return BadRequest(new { error = "At least one data source is required" });
            }

            if (request.SelectedColumns.Count == 0)
            {
                return BadRequest(new { error = "At least one column is required" });
            }

            // Build temporary template for preview
            var previewResult = await ExecuteCustomReportQuery(request, companyId, 100);
            return Ok(previewResult);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Custom report preview failed for company {CompanyId}", companyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new custom report template.
    /// </summary>
    [HttpPost("custom/company/{companyId}")]
    public async Task<ActionResult<ReportTemplateDto>> CreateCustomReport(
        int companyId,
        [FromBody] CreateCustomReportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation(
            "Creating custom report '{Name}' for company {CompanyId} by user {UserId}",
            request.Name,
            companyId,
            user.Id);

        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Report name is required" });
            }

            if (request.DataSources.Count == 0)
            {
                return BadRequest(new { error = "At least one data source is required" });
            }

            if (request.SelectedColumns.Count == 0)
            {
                return BadRequest(new { error = "At least one column is required" });
            }

            // Build template data
            var dataSources = JsonSerializer.Serialize(request.DataSources.Select(s => s.ToLowerInvariant()).ToList());
            var columns = BuildColumnsJson(request.SelectedColumns);
            var filters = BuildFiltersJson(request.Filters);
            var groupings = BuildGroupingsJson(request.GroupBy);
            var sortOrder = BuildSortOrderJson(request.OrderBy);

            // Create the template
            var template = new ReportTemplate
            {
                CompanyId = companyId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DataSources = dataSources,
                Columns = columns,
                Filters = filters,
                Groupings = groupings,
                SortOrder = sortOrder,
                DefaultPageSize = 50,
                IsActive = true,
                IsSystemTemplate = false,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ReportTemplates.Add(template);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created custom report template {TemplateId} '{Name}' for company {CompanyId}",
                template.Id,
                template.Name,
                companyId);

            return CreatedAtAction(nameof(GetTemplate), new { templateId = template.Id, companyId }, new ReportTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = request.Category,
                IsSystemTemplate = false,
                FilterDefinition = template.Filters,
                CreatedAt = template.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create custom report for company {CompanyId}", companyId);
            return BadRequest(new { error = "Failed to create custom report" });
        }
    }

    /// <summary>
    /// Executes a custom report query for preview or export.
    /// </summary>
    private async Task<CustomReportPreviewResult> ExecuteCustomReportQuery(
        CreateCustomReportRequest request,
        int companyId,
        int maxRows)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Build SQL query
        var (sql, parameters) = BuildCustomReportSql(request, companyId, maxRows);

        _logger.LogDebug("Executing custom report SQL: {Sql}", sql);

        // Execute query
        var columns = new List<ReportColumn>();
        var rows = new List<Dictionary<string, object?>>();

        await using var connection = new Npgsql.NpgsqlConnection(_dbContext.Database.GetConnectionString());
        await connection.OpenAsync();

        await using (var cmd = new Npgsql.NpgsqlCommand(sql, connection))
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            // Build column metadata
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var colName = reader.GetName(i);
                var colType = reader.GetFieldType(i);

                var selectedCol = request.SelectedColumns.ElementAtOrDefault(i);
                var displayName = selectedCol?.Alias ?? colName;

                columns.Add(new ReportColumn
                {
                    Name = colName,
                    DisplayName = displayName,
                    DataType = InferDataTypeFromClrType(colType),
                    SourceColumn = selectedCol != null ? $"{selectedCol.SourceTable}.{selectedCol.ColumnName}" : colName
                });
            }

            // Read rows
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    row[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }
        }

        stopwatch.Stop();

        return new CustomReportPreviewResult
        {
            Columns = columns,
            Rows = rows,
            TotalRows = rows.Count,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Builds SQL query for custom report.
    /// </summary>
    private (string sql, Dictionary<string, object?> parameters) BuildCustomReportSql(
        CreateCustomReportRequest request,
        int companyId,
        int limit)
    {
        var parameters = new Dictionary<string, object?>();
        var sb = new System.Text.StringBuilder();

        // SELECT clause
        sb.Append("SELECT ");
        var selectClauses = new List<string>();

        foreach (var col in request.SelectedColumns)
        {
            var tableName = SanitizeIdentifier(col.SourceTable);
            var columnName = SanitizeIdentifier(col.ColumnName);
            var alias = SanitizeIdentifier(col.Alias.Replace(" ", "_").ToLowerInvariant());

            string selectExpr;
            if (!string.IsNullOrEmpty(col.Aggregation) && col.Aggregation != "NONE")
            {
                var aggregation = col.Aggregation.ToUpperInvariant();
                if (!new[] { "SUM", "AVG", "COUNT", "MIN", "MAX" }.Contains(aggregation))
                {
                    throw new InvalidOperationException($"Invalid aggregation function: {col.Aggregation}");
                }
                selectExpr = $"{aggregation}({tableName}.{columnName}) AS {alias}";
            }
            else
            {
                selectExpr = $"{tableName}.{columnName} AS {alias}";
            }

            selectClauses.Add(selectExpr);
        }

        sb.Append(string.Join(", ", selectClauses));

        // FROM clause
        sb.Append(" FROM ");
        var dataSources = request.DataSources.Select(s => SanitizeIdentifier(s.ToLowerInvariant())).ToList();
        sb.Append(BuildFromClauseForCustomReport(dataSources));

        // WHERE clause
        var whereConditions = new List<string>();

        // Company security filter
        var primaryTable = dataSources[0];
        parameters["@company_id"] = companyId;

        switch (primaryTable)
        {
            case "companies":
                whereConditions.Add($"{primaryTable}.id = @company_id");
                break;
            case "barrels":
            case "batches":
            case "orders":
            case "mashbills":
            case "transfers":
            case "tasks":
            case "products":
                whereConditions.Add($"{primaryTable}.company_id = @company_id");
                break;
        }

        // Add custom filters
        int filterIdx = 0;
        foreach (var filter in request.Filters)
        {
            var tableName = SanitizeIdentifier(filter.SourceTable);
            var columnName = SanitizeIdentifier(filter.ColumnName);
            var paramName = $"@filter_{filterIdx}";

            var condition = BuildFilterCondition(tableName, columnName, filter.Operator, paramName, filter.Value);
            if (!string.IsNullOrEmpty(condition.sql))
            {
                if (whereConditions.Count > 0 && filterIdx > 0)
                {
                    whereConditions.Add($"{filter.Logic.ToUpperInvariant()} {condition.sql}");
                }
                else
                {
                    whereConditions.Add(condition.sql);
                }

                if (condition.param != null)
                {
                    parameters[paramName] = condition.param;
                    if (condition.param2 != null)
                    {
                        parameters[$"{paramName}_2"] = condition.param2;
                    }
                }
            }

            filterIdx++;
        }

        if (whereConditions.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.Append(string.Join(" ", whereConditions));
        }

        // GROUP BY clause
        if (request.GroupBy.Count > 0)
        {
            var groupByClauses = request.GroupBy.Select(g =>
                $"{SanitizeIdentifier(g.SourceTable)}.{SanitizeIdentifier(g.ColumnName)}");
            sb.Append(" GROUP BY ");
            sb.Append(string.Join(", ", groupByClauses));
        }

        // ORDER BY clause
        if (request.OrderBy.Count > 0)
        {
            var orderByClauses = request.OrderBy.Select(o =>
            {
                var direction = o.Direction.ToUpperInvariant() == "DESC" ? "DESC" : "ASC";
                return $"{SanitizeIdentifier(o.SourceTable)}.{SanitizeIdentifier(o.ColumnName)} {direction}";
            });
            sb.Append(" ORDER BY ");
            sb.Append(string.Join(", ", orderByClauses));
        }

        // LIMIT
        sb.Append($" LIMIT {limit}");

        return (sb.ToString(), parameters);
    }

    /// <summary>
    /// Builds FROM clause with JOINs for custom report.
    /// </summary>
    private string BuildFromClauseForCustomReport(List<string> dataSources)
    {
        if (dataSources.Count == 0)
        {
            throw new InvalidOperationException("At least one data source is required.");
        }

        var sb = new System.Text.StringBuilder();
        var primaryTable = dataSources[0];
        sb.Append(primaryTable);

        // Known joins between tables
        var tableJoins = new Dictionary<(string, string), string>
        {
            [("barrels", "batches")] = "barrels.batch_id = batches.id AND barrels.company_id = batches.company_id",
            [("batches", "mashbills")] = "batches.mash_bill_id = mashbills.id",
            [("batches", "products")] = "batches.product_id = products.id",
            [("orders", "batches")] = "orders.batch_id = batches.id",
            [("orders", "products")] = "orders.product_id = products.id",
            [("transfers", "barrels")] = "transfers.barrel_id = barrels.id",
        };

        for (int i = 1; i < dataSources.Count; i++)
        {
            var joinTable = dataSources[i];
            string? joinCondition = null;

            // Look for join condition
            foreach (var existing in dataSources.Take(i))
            {
                if (tableJoins.TryGetValue((existing, joinTable), out var cond))
                {
                    joinCondition = cond;
                    break;
                }
                if (tableJoins.TryGetValue((joinTable, existing), out cond))
                {
                    joinCondition = cond;
                    break;
                }
            }

            if (joinCondition != null)
            {
                sb.Append($" LEFT JOIN {joinTable} ON {joinCondition}");
            }
            else
            {
                // Cross join as fallback
                sb.Append($" CROSS JOIN {joinTable}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds filter condition SQL.
    /// </summary>
    private (string sql, object? param, object? param2) BuildFilterCondition(
        string table,
        string column,
        string op,
        string paramName,
        object? value)
    {
        var columnRef = $"{table}.{column}";

        return op.ToUpperInvariant() switch
        {
            "=" => ($"{columnRef} = {paramName}", value, null),
            "!=" => ($"{columnRef} != {paramName}", value, null),
            ">" => ($"{columnRef} > {paramName}", value, null),
            "<" => ($"{columnRef} < {paramName}", value, null),
            ">=" => ($"{columnRef} >= {paramName}", value, null),
            "<=" => ($"{columnRef} <= {paramName}", value, null),
            "LIKE" => ($"{columnRef} ILIKE {paramName}", $"%{value}%", null),
            "IS NULL" => ($"{columnRef} IS NULL", null, null),
            "IS NOT NULL" => ($"{columnRef} IS NOT NULL", null, null),
            "IN" => BuildInCondition(columnRef, paramName, value),
            "BETWEEN" => BuildBetweenCondition(columnRef, paramName, value),
            _ => ("", null, null)
        };
    }

    private (string sql, object? param, object? param2) BuildInCondition(string columnRef, string paramName, object? value)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();
            int idx = 0;
            foreach (var item in jsonElement.EnumerateArray())
            {
                values.Add($"{paramName}_{idx}");
                idx++;
            }
            // For IN, we'd need multiple parameters - simplified for now
            return ($"{columnRef} = ANY({paramName})", value?.ToString()?.Split(','), null);
        }
        return ($"{columnRef} = ANY({paramName})", value?.ToString()?.Split(','), null);
    }

    private (string sql, object? param, object? param2) BuildBetweenCondition(string columnRef, string paramName, object? value)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var arr = jsonElement.EnumerateArray().ToList();
            if (arr.Count >= 2)
            {
                return ($"{columnRef} BETWEEN {paramName} AND {paramName}_2",
                    arr[0].GetString(),
                    arr[1].GetString());
            }
        }
        return ("", null, null);
    }

    /// <summary>
    /// Sanitizes identifier to prevent SQL injection.
    /// </summary>
    private static string SanitizeIdentifier(string identifier)
    {
        // Only allow alphanumeric and underscore
        return System.Text.RegularExpressions.Regex.Replace(identifier.Trim(), @"[^a-zA-Z0-9_]", "").ToLowerInvariant();
    }

    /// <summary>
    /// Builds columns JSON for template.
    /// </summary>
    private static string BuildColumnsJson(List<CustomReportColumn> columns)
    {
        var columnSpecs = columns.Select(c =>
        {
            var tableName = SanitizeIdentifier(c.SourceTable);
            var columnName = SanitizeIdentifier(c.ColumnName);
            var alias = c.Alias.Replace(" ", "_");

            if (!string.IsNullOrEmpty(c.Aggregation) && c.Aggregation != "NONE")
            {
                return $"{c.Aggregation}({tableName}.{columnName}) as {alias}";
            }
            return $"{tableName}.{columnName} as {alias}";
        }).ToList();

        return JsonSerializer.Serialize(columnSpecs);
    }

    /// <summary>
    /// Builds filters JSON for template.
    /// </summary>
    private static string BuildFiltersJson(List<CustomReportFilter> filters)
    {
        if (filters.Count == 0)
        {
            return "{}";
        }

        var filterParts = new List<string>();
        var defaultParams = new Dictionary<string, object?>();
        int paramIdx = 0;

        foreach (var filter in filters)
        {
            var tableName = SanitizeIdentifier(filter.SourceTable);
            var columnName = SanitizeIdentifier(filter.ColumnName);
            var paramName = $"p{paramIdx}";

            var condition = filter.Operator.ToUpperInvariant() switch
            {
                "IS NULL" => $"{tableName}.{columnName} IS NULL",
                "IS NOT NULL" => $"{tableName}.{columnName} IS NOT NULL",
                "LIKE" => $"{tableName}.{columnName} ILIKE @{paramName}",
                _ => $"{tableName}.{columnName} {filter.Operator} @{paramName}"
            };

            if (filterParts.Count > 0)
            {
                filterParts.Add($"{filter.Logic} {condition}");
            }
            else
            {
                filterParts.Add(condition);
            }

            if (filter.Value != null)
            {
                defaultParams[paramName] = filter.Value;
            }

            paramIdx++;
        }

        var filterConfig = new
        {
            filter = string.Join(" ", filterParts),
            defaultParameters = defaultParams
        };

        return JsonSerializer.Serialize(filterConfig);
    }

    /// <summary>
    /// Builds groupings JSON for template.
    /// </summary>
    private static string BuildGroupingsJson(List<CustomReportGroupBy> groupBy)
    {
        if (groupBy.Count == 0)
        {
            return "[]";
        }

        var groupings = groupBy.Select(g =>
            $"{SanitizeIdentifier(g.SourceTable)}.{SanitizeIdentifier(g.ColumnName)}").ToList();

        return JsonSerializer.Serialize(groupings);
    }

    /// <summary>
    /// Builds sort order JSON for template.
    /// </summary>
    private static string BuildSortOrderJson(List<CustomReportOrderBy> orderBy)
    {
        if (orderBy.Count == 0)
        {
            return "[]";
        }

        var sortSpecs = orderBy.Select(o => new
        {
            column = $"{SanitizeIdentifier(o.SourceTable)}.{SanitizeIdentifier(o.ColumnName)}",
            direction = o.Direction.ToLowerInvariant()
        }).ToList();

        return JsonSerializer.Serialize(sortSpecs);
    }

    /// <summary>
    /// Infers data type from CLR type.
    /// </summary>
    private static string InferDataTypeFromClrType(Type type)
    {
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return "datetime";
        if (type == typeof(DateOnly))
            return "date";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return "decimal";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        return "string";
    }
}

/// <summary>
/// Result of custom report preview.
/// </summary>
public class CustomReportPreviewResult
{
    public List<ReportColumn> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public long ExecutionTimeMs { get; set; }
}
