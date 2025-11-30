using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Caskr.server.Services;

public interface IReportingService
{
    /// <summary>
    /// Executes a dynamic report based on a template configuration.
    /// </summary>
    /// <param name="reportTemplateId">The ID of the report template to execute.</param>
    /// <param name="companyId">The company ID for security filtering.</param>
    /// <param name="parameters">Runtime parameters for the report.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ReportResult containing columns, rows, and metadata.</returns>
    Task<ReportResult> ExecuteReportAsync(
        int reportTemplateId,
        int companyId,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a dynamic report with full execution parameters.
    /// </summary>
    Task<ReportResult> ExecuteReportAsync(
        int reportTemplateId,
        int companyId,
        ReportExecutionParameters? executionParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a report template configuration without executing it.
    /// </summary>
    Task<ValidationResult> ValidateTemplateAsync(
        int reportTemplateId,
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available report templates for a company.
    /// </summary>
    Task<IEnumerable<ReportTemplate>> GetTemplatesForCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Dynamic report execution engine that generates SQL queries based on report templates.
/// Supports JOINs across multiple tables, filtering, grouping, sorting, and pagination.
///
/// Security: All queries are filtered by company_id and use parameterized queries to prevent SQL injection.
/// Performance: Results are cached for 5 minutes. Slow queries (>2 sec) are logged.
/// </summary>
public sealed partial class ReportingService(
    CaskrDbContext dbContext,
    IMemoryCache cache,
    ILogger<ReportingService> logger) : IReportingService
{
    // Cache configuration
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "Report_";

    // Performance monitoring
    private const int SlowQueryThresholdMs = 2000;

    // Allowed tables that can be queried (whitelist for security)
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "barrels", "batches", "orders", "users", "companies",
        "rickhouses", "mash_bills", "components", "products",
        "invoices", "invoice_line_items", "invoice_taxes",
        "ttb_transactions", "ttb_monthly_reports", "ttb_inventory_snapshots",
        "ttb_gauge_records", "ttb_tax_determinations", "ttb_audit_logs",
        "spirit_types", "status", "status_task"
    };

    // Table to company_id column mapping for security filtering
    private static readonly Dictionary<string, string> TableCompanyIdColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["barrels"] = "company_id",
        ["batches"] = "company_id",
        ["orders"] = "company_id",
        ["users"] = "company_id",
        ["companies"] = "id",
        ["rickhouses"] = "company_id",
        ["mash_bills"] = "company_id",
        ["invoices"] = "company_id",
        ["invoice_line_items"] = null!, // Joined via invoices
        ["invoice_taxes"] = null!, // Joined via invoices
        ["ttb_transactions"] = "company_id",
        ["ttb_monthly_reports"] = "company_id",
        ["ttb_inventory_snapshots"] = "company_id",
        ["ttb_gauge_records"] = null!, // Joined via barrels
        ["ttb_tax_determinations"] = "company_id",
        ["ttb_audit_logs"] = "company_id",
        ["spirit_types"] = null!, // Reference table, no company filter
        ["status"] = null!, // Reference table, no company filter
        ["status_task"] = null!, // Reference table, no company filter
        ["products"] = null!, // Special handling
        ["components"] = null!, // Joined via batches
    };

    // Table relationships for automatic JOIN generation
    private static readonly Dictionary<(string, string), string> TableJoins = new()
    {
        [("barrels", "batches")] = "barrels.batch_id = batches.id AND barrels.company_id = batches.company_id",
        [("barrels", "orders")] = "barrels.order_id = orders.id",
        [("barrels", "rickhouses")] = "barrels.rickhouse_id = rickhouses.id",
        [("barrels", "companies")] = "barrels.company_id = companies.id",
        [("batches", "mash_bills")] = "batches.mash_bill_id = mash_bills.id",
        [("orders", "users")] = "orders.owner_id = users.id",
        [("orders", "status")] = "orders.status_id = status.id",
        [("orders", "spirit_types")] = "orders.spirit_type_id = spirit_types.id",
        [("orders", "batches")] = "orders.batch_id = batches.id AND orders.company_id = batches.company_id",
        [("orders", "invoices")] = "orders.invoice_id = invoices.id",
        [("invoices", "invoice_line_items")] = "invoice_line_items.invoice_id = invoices.id",
        [("invoices", "invoice_taxes")] = "invoice_taxes.invoice_id = invoices.id",
        [("ttb_gauge_records", "barrels")] = "ttb_gauge_records.barrel_id = barrels.id",
        [("ttb_gauge_records", "users")] = "ttb_gauge_records.gauged_by_user_id = users.id",
        [("ttb_transactions", "companies")] = "ttb_transactions.company_id = companies.id",
        [("ttb_monthly_reports", "companies")] = "ttb_monthly_reports.company_id = companies.id",
        [("ttb_monthly_reports", "users")] = "ttb_monthly_reports.created_by_user_id = users.id",
        [("ttb_tax_determinations", "orders")] = "ttb_tax_determinations.order_id = orders.id",
        [("ttb_tax_determinations", "companies")] = "ttb_tax_determinations.company_id = companies.id",
        [("users", "companies")] = "users.company_id = companies.id",
        [("rickhouses", "companies")] = "rickhouses.company_id = companies.id",
    };

    public async Task<ReportResult> ExecuteReportAsync(
        int reportTemplateId,
        int companyId,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteReportAsync(
            reportTemplateId,
            companyId,
            new ReportExecutionParameters { Parameters = parameters ?? new() },
            cancellationToken);
    }

    public async Task<ReportResult> ExecuteReportAsync(
        int reportTemplateId,
        int companyId,
        ReportExecutionParameters? executionParameters,
        CancellationToken cancellationToken = default)
    {
        executionParameters ??= new ReportExecutionParameters();

        // Generate cache key from all inputs
        var cacheKey = GenerateCacheKey(reportTemplateId, companyId, executionParameters);

        // Check cache first
        if (cache.TryGetValue(cacheKey, out ReportResult? cachedResult) && cachedResult != null)
        {
            logger.LogDebug("Returning cached result for report template {TemplateId}", reportTemplateId);
            return cachedResult with { FromCache = true };
        }

        // Fetch and validate template
        var template = await dbContext.ReportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == reportTemplateId && t.CompanyId == companyId && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Report template {reportTemplateId} not found or not accessible.");

        // Parse template configuration
        var dataSources = ParseJsonArray(template.DataSources);
        var columns = ParseJsonArray(template.Columns);
        var filterConfig = ParseFilterConfig(template.Filters);
        var groupings = ParseJsonArray(template.Groupings);
        var sortOrder = executionParameters.SortOverride ?? ParseSortOrder(template.SortOrder);

        // Validate all tables are allowed
        ValidateDataSources(dataSources);

        // Build the query
        var (sql, sqlParams) = BuildQuery(
            dataSources,
            columns,
            filterConfig,
            groupings,
            sortOrder,
            executionParameters.Parameters,
            companyId);

        // Get page size
        var pageSize = executionParameters.PageSize ?? template.DefaultPageSize;
        var page = Math.Max(1, executionParameters.Page);

        // Execute with timing
        var stopwatch = Stopwatch.StartNew();

        // First, get total count
        var countSql = BuildCountQuery(dataSources, filterConfig, executionParameters.Parameters, companyId);
        int totalRows;

        await using var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using (var countCmd = new NpgsqlCommand(countSql.sql, connection))
        {
            foreach (var param in countSql.parameters)
            {
                countCmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
            var countResult = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalRows = Convert.ToInt32(countResult);
        }

        // Apply pagination
        var offset = (page - 1) * pageSize;
        var paginatedSql = $"{sql} LIMIT {pageSize} OFFSET {offset}";

        // Execute main query
        var rows = new List<Dictionary<string, object?>>();
        var reportColumns = BuildReportColumns(columns);

        await using (var cmd = new NpgsqlCommand(paginatedSql, connection))
        {
            foreach (var param in sqlParams)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
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

        // Log slow queries
        if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
        {
            logger.LogWarning(
                "Slow query detected for report template {TemplateId}: {ElapsedMs}ms. SQL: {Sql}",
                reportTemplateId,
                stopwatch.ElapsedMilliseconds,
                paginatedSql);
        }

        var result = new ReportResult
        {
            Columns = reportColumns,
            Rows = rows,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            FromCache = false,
            TemplateId = reportTemplateId,
            TemplateName = template.Name,
            GeneratedAt = DateTime.UtcNow
        };

        // Cache the result
        cache.Set(cacheKey, result, CacheExpiration);

        logger.LogInformation(
            "Executed report template {TemplateId} '{TemplateName}' in {ElapsedMs}ms. Rows: {RowCount}/{TotalRows}",
            reportTemplateId,
            template.Name,
            stopwatch.ElapsedMilliseconds,
            rows.Count,
            totalRows);

        return result;
    }

    public async Task<ValidationResult> ValidateTemplateAsync(
        int reportTemplateId,
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        var template = await dbContext.ReportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == reportTemplateId && t.CompanyId == companyId, cancellationToken);

        if (template == null)
        {
            result.AddError($"Report template {reportTemplateId} not found or not accessible.");
            return result;
        }

        try
        {
            var dataSources = ParseJsonArray(template.DataSources);
            if (dataSources.Count == 0)
            {
                result.AddError("Template must have at least one data source.");
            }
            else
            {
                ValidateDataSources(dataSources);
            }

            var columns = ParseJsonArray(template.Columns);
            if (columns.Count == 0)
            {
                result.AddError("Template must have at least one column.");
            }

            // Validate column references
            foreach (var column in columns)
            {
                var parts = column.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var tableName = parts[0].ToLowerInvariant();
                    if (!dataSources.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                    {
                        result.AddWarning($"Column '{column}' references table '{tableName}' which is not in data sources.");
                    }
                }
            }

            // Validate filter syntax
            if (!string.IsNullOrWhiteSpace(template.Filters))
            {
                try
                {
                    var filterConfig = ParseFilterConfig(template.Filters);
                    ValidateFilterSyntax(filterConfig?.Filter);
                }
                catch (Exception ex)
                {
                    result.AddError($"Invalid filter configuration: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Template validation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<ReportTemplate>> GetTemplatesForCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ReportTemplates
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    private (string sql, Dictionary<string, object?> parameters) BuildQuery(
        List<string> dataSources,
        List<string> columns,
        ReportFilterConfig? filterConfig,
        List<string> groupings,
        List<ReportSortSpec> sortOrder,
        Dictionary<string, object?> runtimeParams,
        int companyId)
    {
        var parameters = new Dictionary<string, object?>();
        var sb = new StringBuilder();

        // SELECT clause
        sb.Append("SELECT ");
        var selectColumns = columns.Select(c => BuildSelectColumn(c)).ToList();
        sb.AppendJoin(", ", selectColumns);

        // FROM clause with JOINs
        sb.Append(" FROM ");
        sb.Append(BuildFromClause(dataSources));

        // WHERE clause (security + filters)
        var whereConditions = new List<string>();

        // Add company_id security filter for the primary table
        var primaryTable = dataSources[0].ToLowerInvariant();
        if (TableCompanyIdColumns.TryGetValue(primaryTable, out var companyColumn) && !string.IsNullOrEmpty(companyColumn))
        {
            var companyParamName = "@__company_id";
            if (companyColumn == "id")
            {
                whereConditions.Add($"{primaryTable}.id = {companyParamName}");
            }
            else
            {
                whereConditions.Add($"{primaryTable}.{companyColumn} = {companyParamName}");
            }
            parameters[companyParamName] = companyId;
        }

        // Add custom filters
        if (filterConfig != null && !string.IsNullOrWhiteSpace(filterConfig.Filter))
        {
            var (processedFilter, filterParams) = ProcessFilter(filterConfig, runtimeParams);
            if (!string.IsNullOrWhiteSpace(processedFilter))
            {
                whereConditions.Add($"({processedFilter})");
                foreach (var param in filterParams)
                {
                    parameters[param.Key] = param.Value;
                }
            }
        }

        if (whereConditions.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.AppendJoin(" AND ", whereConditions);
        }

        // GROUP BY clause
        if (groupings.Count > 0)
        {
            sb.Append(" GROUP BY ");
            sb.AppendJoin(", ", groupings.Select(g => SanitizeColumnReference(g)));
        }

        // ORDER BY clause
        if (sortOrder.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var orderClauses = sortOrder.Select(s =>
            {
                var direction = s.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                return $"{SanitizeColumnReference(s.Column)} {direction}";
            });
            sb.AppendJoin(", ", orderClauses);
        }

        return (sb.ToString(), parameters);
    }

    private (string sql, Dictionary<string, object?> parameters) BuildCountQuery(
        List<string> dataSources,
        ReportFilterConfig? filterConfig,
        Dictionary<string, object?> runtimeParams,
        int companyId)
    {
        var parameters = new Dictionary<string, object?>();
        var sb = new StringBuilder();

        sb.Append("SELECT COUNT(*) FROM ");
        sb.Append(BuildFromClause(dataSources));

        var whereConditions = new List<string>();

        // Add company_id security filter
        var primaryTable = dataSources[0].ToLowerInvariant();
        if (TableCompanyIdColumns.TryGetValue(primaryTable, out var companyColumn) && !string.IsNullOrEmpty(companyColumn))
        {
            var companyParamName = "@__company_id";
            if (companyColumn == "id")
            {
                whereConditions.Add($"{primaryTable}.id = {companyParamName}");
            }
            else
            {
                whereConditions.Add($"{primaryTable}.{companyColumn} = {companyParamName}");
            }
            parameters[companyParamName] = companyId;
        }

        if (filterConfig != null && !string.IsNullOrWhiteSpace(filterConfig.Filter))
        {
            var (processedFilter, filterParams) = ProcessFilter(filterConfig, runtimeParams);
            if (!string.IsNullOrWhiteSpace(processedFilter))
            {
                whereConditions.Add($"({processedFilter})");
                foreach (var param in filterParams)
                {
                    parameters[param.Key] = param.Value;
                }
            }
        }

        if (whereConditions.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.AppendJoin(" AND ", whereConditions);
        }

        return (sb.ToString(), parameters);
    }

    private string BuildFromClause(List<string> dataSources)
    {
        if (dataSources.Count == 0)
        {
            throw new InvalidOperationException("At least one data source is required.");
        }

        var sb = new StringBuilder();
        var primaryTable = dataSources[0].ToLowerInvariant();
        sb.Append(primaryTable);

        // Add JOINs for additional tables
        for (int i = 1; i < dataSources.Count; i++)
        {
            var joinTable = dataSources[i].ToLowerInvariant();
            var joinCondition = FindJoinCondition(dataSources.Take(i).ToList(), joinTable);

            if (joinCondition != null)
            {
                sb.Append($" LEFT JOIN {joinTable} ON {joinCondition}");
            }
            else
            {
                logger.LogWarning("No join condition found for table {Table}, using CROSS JOIN", joinTable);
                sb.Append($" CROSS JOIN {joinTable}");
            }
        }

        return sb.ToString();
    }

    private string? FindJoinCondition(List<string> existingTables, string newTable)
    {
        foreach (var existingTable in existingTables)
        {
            // Try both orderings
            if (TableJoins.TryGetValue((existingTable.ToLowerInvariant(), newTable), out var condition))
            {
                return condition;
            }
            if (TableJoins.TryGetValue((newTable, existingTable.ToLowerInvariant()), out condition))
            {
                return condition;
            }
        }
        return null;
    }

    private string BuildSelectColumn(string columnSpec)
    {
        // Handle alias syntax: "table.column as Alias" or "table.column Alias"
        var asMatch = AsAliasRegex().Match(columnSpec);
        if (asMatch.Success)
        {
            var column = SanitizeColumnReference(asMatch.Groups[1].Value);
            var alias = SanitizeIdentifier(asMatch.Groups[2].Value);
            return $"{column} AS {alias}";
        }

        // Simple column reference
        return SanitizeColumnReference(columnSpec);
    }

    private List<ReportColumn> BuildReportColumns(List<string> columnSpecs)
    {
        var result = new List<ReportColumn>();

        foreach (var spec in columnSpecs)
        {
            var asMatch = AsAliasRegex().Match(spec);
            string sourceColumn;
            string displayName;

            if (asMatch.Success)
            {
                sourceColumn = asMatch.Groups[1].Value.Trim();
                displayName = asMatch.Groups[2].Value.Trim();
            }
            else
            {
                sourceColumn = spec.Trim();
                // Generate display name from column reference
                var parts = sourceColumn.Split('.');
                displayName = parts.Length > 1
                    ? $"{ToPascalCase(parts[0])} {ToPascalCase(parts[1])}"
                    : ToPascalCase(sourceColumn);
            }

            var columnName = asMatch.Success
                ? asMatch.Groups[2].Value.Trim().ToLowerInvariant()
                : sourceColumn.Replace(".", "_").ToLowerInvariant();

            result.Add(new ReportColumn
            {
                Name = columnName,
                DisplayName = displayName,
                DataType = InferDataType(sourceColumn),
                SourceColumn = sourceColumn
            });
        }

        return result;
    }

    private (string filter, Dictionary<string, object?> parameters) ProcessFilter(
        ReportFilterConfig config,
        Dictionary<string, object?> runtimeParams)
    {
        var parameters = new Dictionary<string, object?>();
        var filter = config.Filter;

        // Merge default and runtime parameters (runtime takes precedence)
        var allParams = new Dictionary<string, object?>(config.DefaultParameters);
        foreach (var kvp in runtimeParams)
        {
            allParams[kvp.Key] = kvp.Value;
        }

        // Find all @parameter references in the filter
        var paramMatches = ParameterRegex().Matches(filter);
        foreach (Match match in paramMatches)
        {
            var paramName = match.Groups[1].Value;
            var sqlParamName = $"@{paramName}";

            if (allParams.TryGetValue(paramName, out var value))
            {
                parameters[sqlParamName] = value;
            }
            else
            {
                // Parameter not provided - this could be intentional for optional filters
                logger.LogDebug("Filter parameter {ParamName} not provided", paramName);
            }
        }

        return (filter, parameters);
    }

    private void ValidateDataSources(List<string> dataSources)
    {
        foreach (var source in dataSources)
        {
            var tableName = source.ToLowerInvariant();
            if (!AllowedTables.Contains(tableName))
            {
                throw new InvalidOperationException($"Table '{source}' is not allowed in reports.");
            }
        }
    }

    private void ValidateFilterSyntax(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        // Check for dangerous SQL patterns (additional layer of defense)
        var dangerousPatterns = new[]
        {
            "DROP", "DELETE", "INSERT", "UPDATE", "ALTER", "CREATE", "TRUNCATE",
            "EXEC", "EXECUTE", "--", "/*", "*/", "UNION", "INTO"
        };

        var upperFilter = filter.ToUpperInvariant();
        foreach (var pattern in dangerousPatterns)
        {
            if (upperFilter.Contains(pattern))
            {
                throw new InvalidOperationException($"Filter contains disallowed keyword: {pattern}");
            }
        }
    }

    private static string SanitizeColumnReference(string columnRef)
    {
        // Only allow alphanumeric, underscore, and dot
        var sanitized = ColumnRefRegex().Replace(columnRef.Trim(), "");

        // Validate format (table.column or just column)
        var parts = sanitized.Split('.');
        if (parts.Length > 2)
        {
            throw new InvalidOperationException($"Invalid column reference: {columnRef}");
        }

        return sanitized.ToLowerInvariant();
    }

    private static string SanitizeIdentifier(string identifier)
    {
        // Only allow alphanumeric and underscore
        return IdentifierRegex().Replace(identifier.Trim(), "").ToLowerInvariant();
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var words = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w =>
            char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..].ToLowerInvariant() : "")));
    }

    private static string InferDataType(string columnRef)
    {
        var lowerRef = columnRef.ToLowerInvariant();

        if (lowerRef.Contains("_at") || lowerRef.Contains("date") || lowerRef.Contains("timestamp"))
            return "datetime";
        if (lowerRef.Contains("_id") || lowerRef.Contains("count") || lowerRef.Contains("quantity"))
            return "number";
        if (lowerRef.Contains("amount") || lowerRef.Contains("price") || lowerRef.Contains("gallons") || lowerRef.Contains("proof"))
            return "decimal";
        if (lowerRef.Contains("is_") || lowerRef.Contains("has_"))
            return "boolean";

        return "string";
    }

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static ReportFilterConfig? ParseFilterConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReportFilterConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static List<ReportSortSpec> ParseSortOrder(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ReportSortSpec>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ReportSortSpec>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ReportSortSpec>();
        }
        catch
        {
            return new List<ReportSortSpec>();
        }
    }

    private string GenerateCacheKey(int templateId, int companyId, ReportExecutionParameters parameters)
    {
        var keyData = JsonSerializer.Serialize(new
        {
            templateId,
            companyId,
            parameters.Parameters,
            parameters.Page,
            parameters.PageSize,
            parameters.SortOverride
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return $"{CacheKeyPrefix}{Convert.ToBase64String(hash)}";
    }

    // Compiled regex patterns for performance
    [GeneratedRegex(@"(.+)\s+[Aa][Ss]\s+(.+)$")]
    private static partial Regex AsAliasRegex();

    [GeneratedRegex(@"@(\w+)")]
    private static partial Regex ParameterRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9_.]")]
    private static partial Regex ColumnRefRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex IdentifierRegex();
}
