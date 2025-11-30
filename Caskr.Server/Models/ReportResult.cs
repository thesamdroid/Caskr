namespace Caskr.server.Models;

/// <summary>
/// Result of executing a dynamic report. Contains column definitions, data rows,
/// pagination info, and execution metadata.
/// </summary>
public sealed class ReportResult
{
    /// <summary>
    /// Column definitions including name, display name, and data type.
    /// </summary>
    public List<ReportColumn> Columns { get; init; } = new();

    /// <summary>
    /// Data rows where each row is a dictionary mapping column names to values.
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; init; } = new();

    /// <summary>
    /// Total number of rows matching the query (before pagination).
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of rows per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRows / PageSize) : 0;

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Whether the result was served from cache.
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Template ID used to generate this report.
    /// </summary>
    public int TemplateId { get; init; }

    /// <summary>
    /// Template name.
    /// </summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Describes a column in the report result.
/// </summary>
public sealed class ReportColumn
{
    /// <summary>
    /// Internal column name (e.g., "barrels_sku" or "batch_name").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Barrel SKU" or "Batch Name").
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Data type for proper formatting (string, number, date, boolean, etc.).
    /// </summary>
    public string DataType { get; init; } = "string";

    /// <summary>
    /// Original source table.column reference.
    /// </summary>
    public string SourceColumn { get; init; } = string.Empty;
}

/// <summary>
/// Parameters for executing a report.
/// </summary>
public sealed class ReportExecutionParameters
{
    /// <summary>
    /// Runtime parameter values to substitute in the filter.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of rows per page. If not specified, uses template default.
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    /// Override sort order at runtime.
    /// </summary>
    public List<ReportSortSpec>? SortOverride { get; init; }
}

/// <summary>
/// Sort specification for a column.
/// </summary>
public sealed class ReportSortSpec
{
    /// <summary>
    /// Column name to sort by.
    /// </summary>
    public string Column { get; init; } = string.Empty;

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// </summary>
    public string Direction { get; init; } = "asc";
}

/// <summary>
/// Filter configuration stored in the template.
/// </summary>
public sealed class ReportFilterConfig
{
    /// <summary>
    /// SQL WHERE clause fragment with @parameter placeholders.
    /// </summary>
    public string Filter { get; init; } = string.Empty;

    /// <summary>
    /// Default parameter values.
    /// </summary>
    public Dictionary<string, object?> DefaultParameters { get; init; } = new();
}
