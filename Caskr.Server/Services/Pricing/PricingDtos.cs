using Caskr.server.Models.Pricing;

namespace Caskr.server.Services.Pricing;

/// <summary>
/// DTO for a pricing tier with its features.
/// </summary>
public class PricingTierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public int? MonthlyPriceCents { get; set; }
    public int? AnnualPriceCents { get; set; }
    public int AnnualDiscountPercent { get; set; }
    public bool IsPopular { get; set; }
    public bool IsCustomPricing { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public int SortOrder { get; set; }
    public List<PricingTierFeatureDto> Features { get; set; } = [];

    /// <summary>
    /// Formatted monthly price (e.g., "$299").
    /// </summary>
    public string? MonthlyPriceFormatted => MonthlyPriceCents.HasValue
        ? $"${MonthlyPriceCents.Value / 100m:N0}"
        : null;

    /// <summary>
    /// Formatted annual price (e.g., "$2,870").
    /// </summary>
    public string? AnnualPriceFormatted => AnnualPriceCents.HasValue
        ? $"${AnnualPriceCents.Value / 100m:N0}"
        : null;

    /// <summary>
    /// Annual savings message (e.g., "Save 20%").
    /// </summary>
    public string? AnnualSavingsMessage => AnnualDiscountPercent > 0
        ? $"Save {AnnualDiscountPercent}%"
        : null;
}

/// <summary>
/// DTO for a feature within a pricing tier.
/// </summary>
public class PricingTierFeatureDto
{
    public int FeatureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsIncluded { get; set; }
    public string? LimitValue { get; set; }
    public string? LimitDescription { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO for a standalone pricing feature.
/// </summary>
public class PricingFeatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO for features grouped by category.
/// </summary>
public class PricingFeatureCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<PricingFeatureDto> Features { get; set; } = [];
}

/// <summary>
/// DTO for a pricing FAQ.
/// </summary>
public class PricingFaqDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO for the complete pricing page data.
/// </summary>
public class PricingPageDataDto
{
    public List<PricingTierDto> Tiers { get; set; } = [];
    public List<PricingFeatureCategoryDto> FeaturesByCategory { get; set; } = [];
    public List<PricingFaqDto> Faqs { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of promo code validation.
/// </summary>
public class PromoCodeValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DiscountType? DiscountType { get; set; }
    public int? DiscountValue { get; set; }
    public List<int>? ApplicableTierIds { get; set; }

    /// <summary>
    /// Human-readable discount description.
    /// </summary>
    public string? DiscountDescription => DiscountType switch
    {
        Models.Pricing.DiscountType.Percentage => $"{DiscountValue}% off",
        Models.Pricing.DiscountType.FixedAmount => $"${(DiscountValue ?? 0) / 100m:N2} off",
        Models.Pricing.DiscountType.FreeMonths => $"{DiscountValue} free month(s)",
        _ => null
    };
}

/// <summary>
/// Result of applying a promo code to a tier.
/// </summary>
public class PromoCodeApplicationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Code { get; set; }
    public int? TierId { get; set; }
    public int? OriginalMonthlyPriceCents { get; set; }
    public int? DiscountedMonthlyPriceCents { get; set; }
    public int? OriginalAnnualPriceCents { get; set; }
    public int? DiscountedAnnualPriceCents { get; set; }
    public int? FreeMonths { get; set; }
    public DiscountType? DiscountType { get; set; }
    public int? DiscountValue { get; set; }

    /// <summary>
    /// Formatted original monthly price.
    /// </summary>
    public string? OriginalMonthlyPriceFormatted => OriginalMonthlyPriceCents.HasValue
        ? $"${OriginalMonthlyPriceCents.Value / 100m:N2}"
        : null;

    /// <summary>
    /// Formatted discounted monthly price.
    /// </summary>
    public string? DiscountedMonthlyPriceFormatted => DiscountedMonthlyPriceCents.HasValue
        ? $"${DiscountedMonthlyPriceCents.Value / 100m:N2}"
        : null;

    /// <summary>
    /// Formatted original annual price.
    /// </summary>
    public string? OriginalAnnualPriceFormatted => OriginalAnnualPriceCents.HasValue
        ? $"${OriginalAnnualPriceCents.Value / 100m:N2}"
        : null;

    /// <summary>
    /// Formatted discounted annual price.
    /// </summary>
    public string? DiscountedAnnualPriceFormatted => DiscountedAnnualPriceCents.HasValue
        ? $"${DiscountedAnnualPriceCents.Value / 100m:N2}"
        : null;
}

/// <summary>
/// Request to validate a promo code.
/// </summary>
public class ValidatePromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public int? TierId { get; set; }
}

/// <summary>
/// Request to apply a promo code.
/// </summary>
public class ApplyPromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public int TierId { get; set; }
}
