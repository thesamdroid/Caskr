using System.Text.Json;
using System.Text.Json.Serialization;
using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services.Pricing;

/// <summary>
/// Interface for logging pricing admin audit entries.
/// </summary>
public interface IPricingAuditLogger
{
    /// <summary>
    /// Logs a change to a pricing entity.
    /// </summary>
    Task LogChangeAsync<T>(PricingAuditAction action, T? entity, T? oldEntity, int userId) where T : class;

    /// <summary>
    /// Gets audit logs for pricing entities.
    /// </summary>
    Task<IEnumerable<PricingAuditLog>> GetAuditLogsAsync(
        string? entityType = null,
        int? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100);
}

/// <summary>
/// Service for logging all admin changes to pricing data.
/// </summary>
public class PricingAuditLoggerService : IPricingAuditLogger
{
    private readonly CaskrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PricingAuditLoggerService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PricingAuditLoggerService(
        CaskrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PricingAuditLoggerService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogChangeAsync<T>(PricingAuditAction action, T? entity, T? oldEntity, int userId) where T : class
    {
        var entityType = GetEntityTypeName<T>();
        var entityId = GetEntityId(action == PricingAuditAction.Delete ? oldEntity : entity);

        if (entityId == 0)
        {
            _logger.LogWarning(
                "Could not determine entity ID for pricing audit log entry. EntityType: {EntityType}, Action: {Action}",
                entityType, action);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        var auditLog = new PricingAuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ChangedByUserId = userId,
            ChangeTimestamp = DateTime.UtcNow,
            OldValues = oldEntity != null ? SerializeEntity(oldEntity) : null,
            NewValues = entity != null ? SerializeEntity(entity) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            ChangeDescription = GenerateChangeDescription(action, entityType, entity, oldEntity)
        };

        _context.PricingAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pricing Audit: {Action} {EntityType} ID {EntityId} by User {UserId}",
            action, entityType, entityId, userId);
    }

    public async Task<IEnumerable<PricingAuditLog>> GetAuditLogsAsync(
        string? entityType = null,
        int? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100)
    {
        var query = _context.PricingAuditLogs
            .Include(a => a.ChangedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId.Value);
        }

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
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    private static string GetEntityTypeName<T>()
    {
        var typeName = typeof(T).Name;
        return typeName switch
        {
            nameof(PricingTier) => "PricingTier",
            nameof(PricingFeature) => "PricingFeature",
            nameof(PricingTierFeature) => "PricingTierFeature",
            nameof(PricingFaq) => "PricingFaq",
            nameof(PricingPromotion) => "PricingPromotion",
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
            var simplified = CreateSimplifiedEntity(entity);
            return JsonSerializer.Serialize(simplified, JsonOptions);
        }
        catch (Exception)
        {
            return $"{{\"type\":\"{typeof(T).Name}\",\"id\":{GetEntityId(entity)}}}";
        }
    }

    private static object CreateSimplifiedEntity<T>(T entity) where T : class
    {
        return entity switch
        {
            PricingTier t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Tagline,
                t.MonthlyPriceCents,
                t.AnnualPriceCents,
                t.AnnualDiscountPercent,
                t.IsPopular,
                t.IsCustomPricing,
                t.CtaText,
                t.CtaUrl,
                t.SortOrder,
                t.IsActive
            },
            PricingFeature f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Category,
                f.SortOrder,
                f.IsActive
            },
            PricingTierFeature tf => new
            {
                tf.Id,
                tf.TierId,
                tf.FeatureId,
                tf.IsIncluded,
                tf.LimitValue,
                tf.LimitDescription
            },
            PricingFaq faq => new
            {
                faq.Id,
                faq.Question,
                faq.Answer,
                faq.SortOrder,
                faq.IsActive
            },
            PricingPromotion p => new
            {
                p.Id,
                p.Code,
                p.Description,
                DiscountType = p.DiscountType.ToString(),
                p.DiscountValue,
                p.AppliesToTiersJson,
                p.ValidFrom,
                p.ValidUntil,
                p.MaxRedemptions,
                p.CurrentRedemptions,
                p.IsActive
            },
            _ => entity
        };
    }

    private static string GenerateChangeDescription<T>(PricingAuditAction action, string entityType, T? entity, T? oldEntity) where T : class
    {
        var actionVerb = action switch
        {
            PricingAuditAction.Create => "created",
            PricingAuditAction.Update => "updated",
            PricingAuditAction.Delete => "deleted",
            PricingAuditAction.Activate => "activated",
            PricingAuditAction.Deactivate => "deactivated",
            _ => "modified"
        };

        return entity switch
        {
            PricingTier t => $"Pricing tier '{t.Name}' ({t.Slug}) was {actionVerb}",
            PricingFeature f => $"Pricing feature '{f.Name}' was {actionVerb}",
            PricingTierFeature tf => $"Tier-feature mapping (Tier {tf.TierId}, Feature {tf.FeatureId}) was {actionVerb}",
            PricingFaq faq => $"FAQ '{TruncateString(faq.Question, 50)}' was {actionVerb}",
            PricingPromotion p => $"Promo code '{p.Code}' was {actionVerb}",
            _ when oldEntity is PricingTier ot => $"Pricing tier '{ot.Name}' ({ot.Slug}) was {actionVerb}",
            _ when oldEntity is PricingFeature of => $"Pricing feature '{of.Name}' was {actionVerb}",
            _ when oldEntity is PricingTierFeature otf => $"Tier-feature mapping (Tier {otf.TierId}, Feature {otf.FeatureId}) was {actionVerb}",
            _ when oldEntity is PricingFaq ofaq => $"FAQ '{TruncateString(ofaq.Question, 50)}' was {actionVerb}",
            _ when oldEntity is PricingPromotion op => $"Promo code '{op.Code}' was {actionVerb}",
            _ => $"{entityType} was {actionVerb}"
        };
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
