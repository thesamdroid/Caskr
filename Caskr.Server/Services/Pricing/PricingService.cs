using System.Text.Json;
using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services.Pricing;

/// <summary>
/// Interface for pricing-related operations.
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Gets all active pricing tiers with their features.
    /// </summary>
    Task<IEnumerable<PricingTierDto>> GetActivePricingTiersAsync();

    /// <summary>
    /// Gets a single tier by its URL-friendly slug.
    /// </summary>
    Task<PricingTierDto?> GetTierBySlugAsync(string slug);

    /// <summary>
    /// Gets all active features grouped by category.
    /// </summary>
    Task<IEnumerable<PricingFeatureCategoryDto>> GetPricingFeaturesAsync();

    /// <summary>
    /// Gets all active FAQs.
    /// </summary>
    Task<IEnumerable<PricingFaqDto>> GetPricingFaqsAsync();

    /// <summary>
    /// Validates a promotional code.
    /// </summary>
    Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string code, int? tierId = null);

    /// <summary>
    /// Applies a promotional code to a tier and calculates the discounted price.
    /// </summary>
    Task<PromoCodeApplicationResult> ApplyPromoCodeAsync(string code, int tierId);

    /// <summary>
    /// Gets aggregated pricing page data (tiers, features, FAQs).
    /// </summary>
    Task<PricingPageDataDto> GetPricingPageDataAsync();
}

/// <summary>
/// Service for managing pricing data and promo code validation.
/// </summary>
public class PricingService : IPricingService
{
    private readonly CaskrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PricingService> _logger;

    private const string CacheKeyTiers = "Pricing:Tiers";
    private const string CacheKeyFeatures = "Pricing:Features";
    private const string CacheKeyFaqs = "Pricing:Faqs";
    private const string CacheKeyPageData = "Pricing:PageData";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PricingService(
        CaskrDbContext context,
        IMemoryCache cache,
        ILogger<PricingService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<PricingTierDto>> GetActivePricingTiersAsync()
    {
        if (_cache.TryGetValue(CacheKeyTiers, out List<PricingTierDto>? cached) && cached != null)
        {
            return cached;
        }

        var tiers = await _context.PricingTiers
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Include(t => t.TierFeatures)
                .ThenInclude(tf => tf.Feature)
            .AsNoTracking()
            .ToListAsync();

        var result = tiers.Select(MapTierToDto).ToList();

        _cache.Set(CacheKeyTiers, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }

    public async Task<PricingTierDto?> GetTierBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        // Try to get from cache first
        var tiers = await GetActivePricingTiersAsync();
        var tier = tiers.FirstOrDefault(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return tier;
    }

    public async Task<IEnumerable<PricingFeatureCategoryDto>> GetPricingFeaturesAsync()
    {
        if (_cache.TryGetValue(CacheKeyFeatures, out List<PricingFeatureCategoryDto>? cached) && cached != null)
        {
            return cached;
        }

        var features = await _context.PricingFeatures
            .Where(f => f.IsActive)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var result = features
            .GroupBy(f => f.Category ?? "Other")
            .Select(g => new PricingFeatureCategoryDto
            {
                Category = g.Key,
                Features = g.Select(f => new PricingFeatureDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    Category = f.Category,
                    SortOrder = f.SortOrder
                }).ToList()
            })
            .ToList();

        _cache.Set(CacheKeyFeatures, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }

    public async Task<IEnumerable<PricingFaqDto>> GetPricingFaqsAsync()
    {
        if (_cache.TryGetValue(CacheKeyFaqs, out List<PricingFaqDto>? cached) && cached != null)
        {
            return cached;
        }

        var faqs = await _context.PricingFaqs
            .Where(f => f.IsActive)
            .OrderBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var result = faqs.Select(f => new PricingFaqDto
        {
            Id = f.Id,
            Question = f.Question,
            Answer = f.Answer,
            SortOrder = f.SortOrder
        }).ToList();

        _cache.Set(CacheKeyFaqs, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }

    public async Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string code, int? tierId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Promo code is required"
            };
        }

        var promo = await _context.PricingPromotions
            .Where(p => p.Code.ToLower() == code.ToLower() && p.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (promo == null)
        {
            _logger.LogInformation("Promo code validation failed: code '{Code}' not found", code);
            return new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid promo code"
            };
        }

        // Check if promo has expired
        var now = DateTime.UtcNow;
        if (promo.ValidFrom.HasValue && now < promo.ValidFrom.Value)
        {
            _logger.LogInformation("Promo code validation failed: code '{Code}' not yet valid", code);
            return new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "This promo code is not yet active"
            };
        }

        if (promo.ValidUntil.HasValue && now > promo.ValidUntil.Value)
        {
            _logger.LogInformation("Promo code validation failed: code '{Code}' has expired", code);
            return new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "This promo code has expired"
            };
        }

        // Check max redemptions
        if (promo.MaxRedemptions.HasValue && promo.CurrentRedemptions >= promo.MaxRedemptions.Value)
        {
            _logger.LogInformation("Promo code validation failed: code '{Code}' max redemptions reached", code);
            return new PromoCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "This promo code has reached its maximum redemptions"
            };
        }

        // Parse applicable tier IDs
        List<int>? applicableTierIds = null;
        if (!string.IsNullOrEmpty(promo.AppliesToTiersJson))
        {
            try
            {
                applicableTierIds = JsonSerializer.Deserialize<List<int>>(promo.AppliesToTiersJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse applies_to_tiers JSON for promo '{Code}'", code);
            }
        }

        // Check if tier is applicable
        if (tierId.HasValue && applicableTierIds != null && applicableTierIds.Count > 0)
        {
            if (!applicableTierIds.Contains(tierId.Value))
            {
                _logger.LogInformation("Promo code validation failed: code '{Code}' not applicable to tier {TierId}", code, tierId);
                return new PromoCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "This promo code is not applicable to the selected tier"
                };
            }
        }

        _logger.LogInformation("Promo code '{Code}' validated successfully", code);
        return new PromoCodeValidationResult
        {
            IsValid = true,
            Code = promo.Code,
            Description = promo.Description,
            DiscountType = promo.DiscountType,
            DiscountValue = promo.DiscountValue,
            ApplicableTierIds = applicableTierIds
        };
    }

    public async Task<PromoCodeApplicationResult> ApplyPromoCodeAsync(string code, int tierId)
    {
        var validation = await ValidatePromoCodeAsync(code, tierId);
        if (!validation.IsValid)
        {
            return new PromoCodeApplicationResult
            {
                Success = false,
                ErrorMessage = validation.ErrorMessage
            };
        }

        var tier = await _context.PricingTiers
            .Where(t => t.Id == tierId && t.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (tier == null)
        {
            return new PromoCodeApplicationResult
            {
                Success = false,
                ErrorMessage = "Tier not found"
            };
        }

        if (tier.IsCustomPricing)
        {
            return new PromoCodeApplicationResult
            {
                Success = false,
                ErrorMessage = "Promo codes cannot be applied to custom pricing tiers"
            };
        }

        var result = new PromoCodeApplicationResult
        {
            Success = true,
            Code = validation.Code,
            TierId = tierId,
            OriginalMonthlyPriceCents = tier.MonthlyPriceCents,
            OriginalAnnualPriceCents = tier.AnnualPriceCents,
            DiscountType = validation.DiscountType,
            DiscountValue = validation.DiscountValue
        };

        switch (validation.DiscountType)
        {
            case DiscountType.Percentage:
                if (tier.MonthlyPriceCents.HasValue)
                {
                    var monthlyDiscount = (int)(tier.MonthlyPriceCents.Value * validation.DiscountValue!.Value / 100m);
                    result.DiscountedMonthlyPriceCents = tier.MonthlyPriceCents.Value - monthlyDiscount;
                }
                if (tier.AnnualPriceCents.HasValue)
                {
                    var annualDiscount = (int)(tier.AnnualPriceCents.Value * validation.DiscountValue!.Value / 100m);
                    result.DiscountedAnnualPriceCents = tier.AnnualPriceCents.Value - annualDiscount;
                }
                break;

            case DiscountType.FixedAmount:
                if (tier.MonthlyPriceCents.HasValue)
                {
                    result.DiscountedMonthlyPriceCents = Math.Max(0, tier.MonthlyPriceCents.Value - validation.DiscountValue!.Value);
                }
                if (tier.AnnualPriceCents.HasValue)
                {
                    result.DiscountedAnnualPriceCents = Math.Max(0, tier.AnnualPriceCents.Value - validation.DiscountValue!.Value);
                }
                break;

            case DiscountType.FreeMonths:
                result.FreeMonths = validation.DiscountValue;
                result.DiscountedMonthlyPriceCents = tier.MonthlyPriceCents;
                result.DiscountedAnnualPriceCents = tier.AnnualPriceCents;
                break;
        }

        _logger.LogInformation(
            "Promo code '{Code}' applied to tier {TierId}: {DiscountType} {DiscountValue}",
            code, tierId, validation.DiscountType, validation.DiscountValue);

        return result;
    }

    public async Task<PricingPageDataDto> GetPricingPageDataAsync()
    {
        if (_cache.TryGetValue(CacheKeyPageData, out PricingPageDataDto? cached) && cached != null)
        {
            return cached;
        }

        var tiers = await GetActivePricingTiersAsync();
        var features = await GetPricingFeaturesAsync();
        var faqs = await GetPricingFaqsAsync();

        var result = new PricingPageDataDto
        {
            Tiers = tiers.ToList(),
            FeaturesByCategory = features.ToList(),
            Faqs = faqs.ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(CacheKeyPageData, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }

    /// <summary>
    /// Invalidates all pricing-related caches.
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Remove(CacheKeyTiers);
        _cache.Remove(CacheKeyFeatures);
        _cache.Remove(CacheKeyFaqs);
        _cache.Remove(CacheKeyPageData);
        _logger.LogInformation("Pricing cache invalidated");
    }

    private static PricingTierDto MapTierToDto(PricingTier tier)
    {
        return new PricingTierDto
        {
            Id = tier.Id,
            Name = tier.Name,
            Slug = tier.Slug,
            Tagline = tier.Tagline,
            MonthlyPriceCents = tier.MonthlyPriceCents,
            AnnualPriceCents = tier.AnnualPriceCents,
            AnnualDiscountPercent = tier.AnnualDiscountPercent,
            IsPopular = tier.IsPopular,
            IsCustomPricing = tier.IsCustomPricing,
            CtaText = tier.CtaText,
            CtaUrl = tier.CtaUrl,
            SortOrder = tier.SortOrder,
            Features = tier.TierFeatures
                .Where(tf => tf.Feature.IsActive)
                .OrderBy(tf => tf.Feature.Category)
                .ThenBy(tf => tf.Feature.SortOrder)
                .Select(tf => new PricingTierFeatureDto
                {
                    FeatureId = tf.FeatureId,
                    Name = tf.Feature.Name,
                    Description = tf.Feature.Description,
                    Category = tf.Feature.Category,
                    IsIncluded = tf.IsIncluded,
                    LimitValue = tf.LimitValue,
                    LimitDescription = tf.LimitDescription,
                    SortOrder = tf.Feature.SortOrder
                })
                .ToList()
        };
    }
}
