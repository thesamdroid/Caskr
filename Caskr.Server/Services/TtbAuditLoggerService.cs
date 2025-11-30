using System.Text.Json;
using System.Text.Json.Serialization;
using Caskr.server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

/// <summary>
/// Interface for logging audit trail entries for TTB compliance.
/// </summary>
public interface ITtbAuditLogger
{
    /// <summary>
    /// Logs a change to a TTB entity.
    /// </summary>
    /// <typeparam name="T">The type of entity being logged.</typeparam>
    /// <param name="action">The action performed (Create, Update, Delete).</param>
    /// <param name="entity">The entity after the change (required for Create/Update, null for Delete).</param>
    /// <param name="oldEntity">The entity before the change (required for Update/Delete, null for Create).</param>
    /// <param name="userId">The ID of the user who made the change.</param>
    /// <param name="companyId">The company ID for the audit entry.</param>
    Task LogChangeAsync<T>(TtbAuditAction action, T? entity, T? oldEntity, int userId, int companyId) where T : class;

    /// <summary>
    /// Gets audit logs for a specific company within a date range.
    /// </summary>
    Task<IEnumerable<TtbAuditLog>> GetAuditLogsAsync(int companyId, DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Exports audit logs to CSV format.
    /// </summary>
    Task<string> ExportAuditLogsToCsvAsync(int companyId, DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Checks if a month has a submitted or approved report (immutability check).
    /// </summary>
    Task<bool> IsMonthLockedAsync(int companyId, int month, int year);
}

/// <summary>
/// Service for logging all changes to TTB compliance data.
/// Provides complete audit trail for TTB inspections.
/// </summary>
public class TtbAuditLoggerService : ITtbAuditLogger
{
    private readonly CaskrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TtbAuditLoggerService> _logger;
    private readonly IUsersService _usersService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TtbAuditLoggerService(
        CaskrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TtbAuditLoggerService> logger,
        IUsersService usersService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _usersService = usersService;
    }

    public async Task LogChangeAsync<T>(TtbAuditAction action, T? entity, T? oldEntity, int userId, int companyId) where T : class
    {
        var entityType = GetEntityTypeName<T>();
        var entityId = GetEntityId(action == TtbAuditAction.Delete ? oldEntity : entity);

        if (entityId == 0)
        {
            _logger.LogWarning("Could not determine entity ID for audit log entry. EntityType: {EntityType}, Action: {Action}", entityType, action);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        var auditLog = new TtbAuditLog
        {
            CompanyId = companyId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ChangedByUserId = userId,
            ChangeTimestamp = DateTime.UtcNow,
            OldValues = oldEntity != null ? SerializeEntity(oldEntity) : null,
            NewValues = entity != null ? SerializeEntity(entity) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            ChangeDescription = await GenerateChangeDescriptionAsync(action, entityType, entity, oldEntity, userId)
        };

        _context.TtbAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "TTB Audit: {Action} {EntityType} ID {EntityId} by User {UserId} for Company {CompanyId}",
            action, entityType, entityId, userId, companyId);
    }

    public async Task<IEnumerable<TtbAuditLog>> GetAuditLogsAsync(int companyId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.TtbAuditLogs
            .Include(a => a.ChangedByUser)
            .Where(a => a.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.ChangeTimestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.ChangeTimestamp <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.ChangeTimestamp)
            .ToListAsync();
    }

    public async Task<string> ExportAuditLogsToCsvAsync(int companyId, DateTime? startDate, DateTime? endDate)
    {
        var logs = await GetAuditLogsAsync(companyId, startDate, endDate);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Timestamp (UTC),User,Entity Type,Entity ID,Action,Change Description,IP Address,User Agent");

        foreach (var log in logs)
        {
            var timestamp = log.ChangeTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var userName = log.ChangedByUser?.Name ?? $"User {log.ChangedByUserId}";
            var description = EscapeCsvField(log.ChangeDescription ?? "");
            var ipAddress = log.IpAddress ?? "";
            var userAgent = EscapeCsvField(log.UserAgent ?? "");

            csv.AppendLine($"{timestamp},{EscapeCsvField(userName)},{log.EntityType},{log.EntityId},{log.Action},{description},{ipAddress},{userAgent}");
        }

        return csv.ToString();
    }

    public async Task<bool> IsMonthLockedAsync(int companyId, int month, int year)
    {
        return await _context.TtbMonthlyReports
            .AnyAsync(r => r.CompanyId == companyId
                && r.ReportMonth == month
                && r.ReportYear == year
                && (r.Status == TtbReportStatus.Submitted || r.Status == TtbReportStatus.Approved));
    }

    private static string GetEntityTypeName<T>()
    {
        var typeName = typeof(T).Name;

        // Handle known entity types
        return typeName switch
        {
            nameof(TtbTransaction) => "TtbTransaction",
            nameof(TtbMonthlyReport) => "TtbMonthlyReport",
            nameof(TtbGaugeRecord) => "TtbGaugeRecord",
            nameof(TtbTaxDetermination) => "TtbTaxDetermination",
            _ => typeName
        };
    }

    private static int GetEntityId<T>(T? entity) where T : class
    {
        if (entity == null) return 0;

        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is int id)
        {
            return id;
        }

        return 0;
    }

    private static string SerializeEntity<T>(T entity) where T : class
    {
        try
        {
            // Create an anonymous object with only the relevant properties to avoid circular references
            var simplified = CreateSimplifiedEntity(entity);
            return JsonSerializer.Serialize(simplified, JsonOptions);
        }
        catch (Exception)
        {
            // Fallback to a basic representation
            return $"{{\"type\":\"{typeof(T).Name}\",\"id\":{GetEntityId(entity)}}}";
        }
    }

    private static object CreateSimplifiedEntity<T>(T entity) where T : class
    {
        return entity switch
        {
            TtbTransaction t => new
            {
                t.Id,
                t.CompanyId,
                t.TransactionDate,
                TransactionType = t.TransactionType.ToString(),
                t.ProductType,
                SpiritsType = t.SpiritsType.ToString(),
                t.ProofGallons,
                t.WineGallons,
                t.SourceEntityType,
                t.SourceEntityId,
                t.Notes
            },
            TtbMonthlyReport r => new
            {
                r.Id,
                r.CompanyId,
                r.ReportMonth,
                r.ReportYear,
                Status = r.Status.ToString(),
                FormType = r.FormType.ToString(),
                r.GeneratedAt,
                r.SubmittedAt,
                r.TtbConfirmationNumber,
                r.PdfPath,
                r.ValidationErrors,
                r.ValidationWarnings,
                r.CreatedByUserId
            },
            TtbGaugeRecord g => new
            {
                g.Id,
                g.BarrelId,
                g.GaugeDate,
                GaugeType = g.GaugeType.ToString(),
                g.Proof,
                g.Temperature,
                g.WineGallons,
                g.ProofGallons,
                g.GaugedByUserId,
                g.Notes
            },
            TtbTaxDetermination td => new
            {
                td.Id,
                td.CompanyId,
                td.OrderId,
                td.ProofGallons,
                td.TaxRate,
                td.TaxAmount,
                td.DeterminationDate,
                td.PaidDate,
                td.PaymentReference,
                td.QuickBooksJournalEntryId,
                td.Notes
            },
            _ => entity
        };
    }

    private async Task<string> GenerateChangeDescriptionAsync<T>(TtbAuditAction action, string entityType, T? entity, T? oldEntity, int userId) where T : class
    {
        var user = await _usersService.GetUserByIdAsync(userId);
        var userName = user?.Name ?? $"User {userId}";

        var actionVerb = action switch
        {
            TtbAuditAction.Create => "created",
            TtbAuditAction.Update => "updated",
            TtbAuditAction.Delete => "deleted",
            _ => "modified"
        };

        return entity switch
        {
            TtbTransaction t => GenerateTransactionDescription(actionVerb, userName, t, oldEntity as TtbTransaction),
            TtbMonthlyReport r => $"{userName} {actionVerb} TTB Monthly Report for {r.ReportMonth}/{r.ReportYear}",
            TtbGaugeRecord g => GenerateGaugeRecordDescription(actionVerb, userName, g, oldEntity as TtbGaugeRecord),
            TtbTaxDetermination td => $"{userName} {actionVerb} Tax Determination for Order #{td.OrderId} ({td.ProofGallons:F2} proof gallons, ${td.TaxAmount:F2})",
            _ when oldEntity is TtbTransaction ot => GenerateTransactionDescription(actionVerb, userName, null, ot),
            _ when oldEntity is TtbMonthlyReport or => $"{userName} {actionVerb} TTB Monthly Report for {or.ReportMonth}/{or.ReportYear}",
            _ when oldEntity is TtbGaugeRecord og => GenerateGaugeRecordDescription(actionVerb, userName, null, og),
            _ when oldEntity is TtbTaxDetermination otd => $"{userName} {actionVerb} Tax Determination for Order #{otd.OrderId}",
            _ => $"{userName} {actionVerb} {entityType}"
        };
    }

    private static string GenerateTransactionDescription(string actionVerb, string userName, TtbTransaction? entity, TtbTransaction? oldEntity)
    {
        var transaction = entity ?? oldEntity;
        if (transaction == null) return $"{userName} {actionVerb} transaction";

        var dateStr = transaction.TransactionDate.ToString("MM/dd/yyyy");
        var typeStr = transaction.TransactionType.ToString();
        var gallons = transaction.ProofGallons;

        return $"{userName} {actionVerb} {typeStr} transaction for {gallons:F2} proof gallons on {dateStr}";
    }

    private static string GenerateGaugeRecordDescription(string actionVerb, string userName, TtbGaugeRecord? entity, TtbGaugeRecord? oldEntity)
    {
        var record = entity ?? oldEntity;
        if (record == null) return $"{userName} {actionVerb} gauge record";

        var dateStr = record.GaugeDate.ToString("MM/dd/yyyy");
        var typeStr = record.GaugeType.ToString();
        var gallons = record.ProofGallons;

        return $"{userName} {actionVerb} {typeStr} gauge record for Barrel #{record.BarrelId} ({gallons:F2} proof gallons) on {dateStr}";
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Try to get IP from X-Forwarded-For header (for proxied requests)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // The X-Forwarded-For header can contain multiple IPs; the first one is the client
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Fall back to the remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        // If the field contains comma, newline, or quotes, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('\n') || field.Contains('\r') || field.Contains('"'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
