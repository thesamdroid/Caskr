using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Caskr.server.Services.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Services;

public class PricingServiceTests : IDisposable
{
    private readonly CaskrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<PricingService>> _loggerMock;
    private readonly PricingService _service;

    public PricingServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<PricingService>>();

        _service = new PricingService(_context, _cache, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed test features
        var complianceFeature = new PricingFeature
        {
            Id = 1,
            Name = "TTB Compliance",
            Description = "TTB compliance automation",
            Category = "Compliance",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inventoryFeature = new PricingFeature
        {
            Id = 2,
            Name = "Barrel Management",
            Description = "Full barrel tracking",
            Category = "Inventory",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveFeature = new PricingFeature
        {
            Id = 3,
            Name = "Deprecated Feature",
            Description = "No longer available",
            Category = "Legacy",
            SortOrder = 1,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PricingFeatures.AddRange(complianceFeature, inventoryFeature, inactiveFeature);

        // Seed test tiers
        var craftTier = new PricingTier
        {
            Id = 1,
            Name = "Craft",
            Slug = "craft",
            Tagline = "Perfect for craft distilleries",
            MonthlyPriceCents = 29900,
            AnnualPriceCents = 287040,
            AnnualDiscountPercent = 20,
            IsPopular = false,
            IsCustomPricing = false,
            CtaText = "Start Free Trial",
            CtaUrl = "/signup?plan=craft",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var growthTier = new PricingTier
        {
            Id = 2,
            Name = "Growth",
            Slug = "growth",
            Tagline = "Scale your operations",
            MonthlyPriceCents = 149900,
            AnnualPriceCents = 1439040,
            AnnualDiscountPercent = 20,
            IsPopular = true,
            IsCustomPricing = false,
            CtaText = "Start Free Trial",
            CtaUrl = "/signup?plan=growth",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var enterpriseTier = new PricingTier
        {
            Id = 3,
            Name = "Enterprise",
            Slug = "enterprise",
            Tagline = "Custom solutions",
            MonthlyPriceCents = null,
            AnnualPriceCents = null,
            AnnualDiscountPercent = 0,
            IsPopular = false,
            IsCustomPricing = true,
            CtaText = "Contact Sales",
            CtaUrl = "/contact",
            SortOrder = 4,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveTier = new PricingTier
        {
            Id = 4,
            Name = "Legacy",
            Slug = "legacy",
            Tagline = "No longer available",
            MonthlyPriceCents = 9900,
            AnnualPriceCents = 95040,
            AnnualDiscountPercent = 20,
            IsPopular = false,
            IsCustomPricing = false,
            CtaText = "Unavailable",
            CtaUrl = "#",
            SortOrder = 5,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PricingTiers.AddRange(craftTier, growthTier, enterpriseTier, inactiveTier);

        // Seed tier features
        var craftCompliance = new PricingTierFeature
        {
            Id = 1,
            TierId = 1,
            FeatureId = 1,
            IsIncluded = true,
            LimitValue = "Basic",
            LimitDescription = null
        };

        var craftInventory = new PricingTierFeature
        {
            Id = 2,
            TierId = 1,
            FeatureId = 2,
            IsIncluded = true,
            LimitValue = "500 barrels",
            LimitDescription = null
        };

        var growthCompliance = new PricingTierFeature
        {
            Id = 3,
            TierId = 2,
            FeatureId = 1,
            IsIncluded = true,
            LimitValue = "Full",
            LimitDescription = null
        };

        var growthInventory = new PricingTierFeature
        {
            Id = 4,
            TierId = 2,
            FeatureId = 2,
            IsIncluded = true,
            LimitValue = "Unlimited",
            LimitDescription = null
        };

        _context.PricingTierFeatures.AddRange(craftCompliance, craftInventory, growthCompliance, growthInventory);

        // Seed FAQs
        var faq1 = new PricingFaq
        {
            Id = 1,
            Question = "What is included in the free trial?",
            Answer = "Full access to Growth tier for 14 days.",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var faq2 = new PricingFaq
        {
            Id = 2,
            Question = "Can I change my plan?",
            Answer = "Yes, you can upgrade or downgrade anytime.",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveFaq = new PricingFaq
        {
            Id = 3,
            Question = "Old question?",
            Answer = "Deprecated answer.",
            SortOrder = 3,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PricingFaqs.AddRange(faq1, faq2, inactiveFaq);

        // Seed promotions
        var validPromo = new PricingPromotion
        {
            Id = 1,
            Code = "WELCOME20",
            Description = "20% off first year",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20,
            AppliesToTiersJson = null,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(30),
            MaxRedemptions = 100,
            CurrentRedemptions = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expiredPromo = new PricingPromotion
        {
            Id = 2,
            Code = "EXPIRED",
            Description = "Expired promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            AppliesToTiersJson = null,
            ValidFrom = DateTime.UtcNow.AddDays(-60),
            ValidUntil = DateTime.UtcNow.AddDays(-30),
            MaxRedemptions = null,
            CurrentRedemptions = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var maxedOutPromo = new PricingPromotion
        {
            Id = 3,
            Code = "MAXEDOUT",
            Description = "Max redemptions reached",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5000,
            AppliesToTiersJson = null,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(30),
            MaxRedemptions = 10,
            CurrentRedemptions = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tierRestrictedPromo = new PricingPromotion
        {
            Id = 4,
            Code = "GROWTHONLY",
            Description = "Only for Growth tier",
            DiscountType = DiscountType.FreeMonths,
            DiscountValue = 2,
            AppliesToTiersJson = "[2]", // Only tier ID 2 (Growth)
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(30),
            MaxRedemptions = null,
            CurrentRedemptions = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PricingPromotions.AddRange(validPromo, expiredPromo, maxedOutPromo, tierRestrictedPromo);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    #region GetActivePricingTiersAsync Tests

    [Fact]
    public async Task GetActivePricingTiersAsync_ReturnsOnlyActiveTiers()
    {
        var tiers = (await _service.GetActivePricingTiersAsync()).ToList();

        Assert.Equal(3, tiers.Count);
        Assert.DoesNotContain(tiers, t => t.Slug == "legacy");
    }

    [Fact]
    public async Task GetActivePricingTiersAsync_ReturnsTiersInSortOrder()
    {
        var tiers = (await _service.GetActivePricingTiersAsync()).ToList();

        Assert.Equal("craft", tiers[0].Slug);
        Assert.Equal("growth", tiers[1].Slug);
        Assert.Equal("enterprise", tiers[2].Slug);
    }

    [Fact]
    public async Task GetActivePricingTiersAsync_IncludesFeatures()
    {
        var tiers = (await _service.GetActivePricingTiersAsync()).ToList();
        var craftTier = tiers.First(t => t.Slug == "craft");

        Assert.Equal(2, craftTier.Features.Count);
        Assert.Contains(craftTier.Features, f => f.Name == "TTB Compliance");
        Assert.Contains(craftTier.Features, f => f.Name == "Barrel Management");
    }

    [Fact]
    public async Task GetActivePricingTiersAsync_CachesResults()
    {
        // First call
        await _service.GetActivePricingTiersAsync();

        // Modify data
        var tier = await _context.PricingTiers.FindAsync(1);
        tier!.Name = "Modified Craft";
        await _context.SaveChangesAsync();

        // Second call should return cached data
        var tiers = (await _service.GetActivePricingTiersAsync()).ToList();
        var craftTier = tiers.First(t => t.Slug == "craft");

        Assert.Equal("Craft", craftTier.Name); // Still the old name from cache
    }

    #endregion

    #region GetTierBySlugAsync Tests

    [Fact]
    public async Task GetTierBySlugAsync_ReturnsTierForValidSlug()
    {
        var tier = await _service.GetTierBySlugAsync("craft");

        Assert.NotNull(tier);
        Assert.Equal("Craft", tier.Name);
    }

    [Fact]
    public async Task GetTierBySlugAsync_ReturnsNullForInvalidSlug()
    {
        var tier = await _service.GetTierBySlugAsync("nonexistent");

        Assert.Null(tier);
    }

    [Fact]
    public async Task GetTierBySlugAsync_ReturnsNullForEmptySlug()
    {
        var tier = await _service.GetTierBySlugAsync("");

        Assert.Null(tier);
    }

    [Fact]
    public async Task GetTierBySlugAsync_IsCaseInsensitive()
    {
        var tier = await _service.GetTierBySlugAsync("CRAFT");

        Assert.NotNull(tier);
        Assert.Equal("Craft", tier.Name);
    }

    #endregion

    #region GetPricingFeaturesAsync Tests

    [Fact]
    public async Task GetPricingFeaturesAsync_ReturnsOnlyActiveFeatures()
    {
        var categories = (await _service.GetPricingFeaturesAsync()).ToList();
        var allFeatures = categories.SelectMany(c => c.Features).ToList();

        Assert.Equal(2, allFeatures.Count);
        Assert.DoesNotContain(allFeatures, f => f.Name == "Deprecated Feature");
    }

    [Fact]
    public async Task GetPricingFeaturesAsync_GroupsByCategory()
    {
        var categories = (await _service.GetPricingFeaturesAsync()).ToList();

        Assert.Equal(2, categories.Count);
        Assert.Contains(categories, c => c.Category == "Compliance");
        Assert.Contains(categories, c => c.Category == "Inventory");
    }

    #endregion

    #region GetPricingFaqsAsync Tests

    [Fact]
    public async Task GetPricingFaqsAsync_ReturnsOnlyActiveFaqs()
    {
        var faqs = (await _service.GetPricingFaqsAsync()).ToList();

        Assert.Equal(2, faqs.Count);
        Assert.DoesNotContain(faqs, f => f.Question.Contains("Old question"));
    }

    [Fact]
    public async Task GetPricingFaqsAsync_ReturnsFaqsInSortOrder()
    {
        var faqs = (await _service.GetPricingFaqsAsync()).ToList();

        Assert.Equal("What is included in the free trial?", faqs[0].Question);
        Assert.Equal("Can I change my plan?", faqs[1].Question);
    }

    #endregion

    #region ValidatePromoCodeAsync Tests

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsValidForActivePromo()
    {
        var result = await _service.ValidatePromoCodeAsync("WELCOME20");

        Assert.True(result.IsValid);
        Assert.Equal("WELCOME20", result.Code);
        Assert.Equal(DiscountType.Percentage, result.DiscountType);
        Assert.Equal(20, result.DiscountValue);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsInvalidForEmptyCode()
    {
        var result = await _service.ValidatePromoCodeAsync("");

        Assert.False(result.IsValid);
        Assert.Equal("Promo code is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsInvalidForNonexistentCode()
    {
        var result = await _service.ValidatePromoCodeAsync("NOTREAL");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid promo code", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsInvalidForExpiredPromo()
    {
        var result = await _service.ValidatePromoCodeAsync("EXPIRED");

        Assert.False(result.IsValid);
        Assert.Equal("This promo code has expired", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsInvalidForMaxRedemptionsReached()
    {
        var result = await _service.ValidatePromoCodeAsync("MAXEDOUT");

        Assert.False(result.IsValid);
        Assert.Equal("This promo code has reached its maximum redemptions", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsInvalidForWrongTier()
    {
        var result = await _service.ValidatePromoCodeAsync("GROWTHONLY", tierId: 1); // Craft tier

        Assert.False(result.IsValid);
        Assert.Equal("This promo code is not applicable to the selected tier", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_ReturnsValidForCorrectTier()
    {
        var result = await _service.ValidatePromoCodeAsync("GROWTHONLY", tierId: 2); // Growth tier

        Assert.True(result.IsValid);
        Assert.Equal(DiscountType.FreeMonths, result.DiscountType);
        Assert.Equal(2, result.DiscountValue);
    }

    [Fact]
    public async Task ValidatePromoCodeAsync_IsCaseInsensitive()
    {
        var result = await _service.ValidatePromoCodeAsync("welcome20");

        Assert.True(result.IsValid);
    }

    #endregion

    #region ApplyPromoCodeAsync Tests

    [Fact]
    public async Task ApplyPromoCodeAsync_CalculatesPercentageDiscountCorrectly()
    {
        var result = await _service.ApplyPromoCodeAsync("WELCOME20", 1); // Craft tier

        Assert.True(result.Success);
        Assert.Equal(29900, result.OriginalMonthlyPriceCents);
        Assert.Equal(23920, result.DiscountedMonthlyPriceCents); // 20% off
        Assert.Equal(287040, result.OriginalAnnualPriceCents);
        Assert.Equal(229632, result.DiscountedAnnualPriceCents); // 20% off
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_CalculatesFixedAmountDiscountCorrectly()
    {
        // Add a fixed amount promo for testing
        var fixedPromo = new PricingPromotion
        {
            Id = 10,
            Code = "FIXED50",
            Description = "$50 off",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5000, // $50 in cents
            AppliesToTiersJson = null,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(30),
            MaxRedemptions = null,
            CurrentRedemptions = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PricingPromotions.Add(fixedPromo);
        await _context.SaveChangesAsync();

        var result = await _service.ApplyPromoCodeAsync("FIXED50", 1);

        Assert.True(result.Success);
        Assert.Equal(29900, result.OriginalMonthlyPriceCents);
        Assert.Equal(24900, result.DiscountedMonthlyPriceCents); // $50 off
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_HandlesFreeMonsthsCorrectly()
    {
        var result = await _service.ApplyPromoCodeAsync("GROWTHONLY", 2);

        Assert.True(result.Success);
        Assert.Equal(2, result.FreeMonths);
        Assert.Equal(result.OriginalMonthlyPriceCents, result.DiscountedMonthlyPriceCents);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_ReturnsErrorForInvalidCode()
    {
        var result = await _service.ApplyPromoCodeAsync("INVALID", 1);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_ReturnsErrorForNonexistentTier()
    {
        var result = await _service.ApplyPromoCodeAsync("WELCOME20", 999);

        Assert.False(result.Success);
        Assert.Equal("Tier not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_ReturnsErrorForCustomPricingTier()
    {
        var result = await _service.ApplyPromoCodeAsync("WELCOME20", 3); // Enterprise tier

        Assert.False(result.Success);
        Assert.Equal("Promo codes cannot be applied to custom pricing tiers", result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyPromoCodeAsync_RespectsAppliesToTiersRestriction()
    {
        var result = await _service.ApplyPromoCodeAsync("GROWTHONLY", 1); // Craft tier

        Assert.False(result.Success);
    }

    #endregion

    #region GetPricingPageDataAsync Tests

    [Fact]
    public async Task GetPricingPageDataAsync_ReturnsCompleteData()
    {
        var data = await _service.GetPricingPageDataAsync();

        Assert.NotNull(data);
        Assert.Equal(3, data.Tiers.Count);
        Assert.Equal(2, data.FeaturesByCategory.Count);
        Assert.Equal(2, data.Faqs.Count);
        Assert.True(data.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPricingPageDataAsync_CachesResults()
    {
        // First call
        var data1 = await _service.GetPricingPageDataAsync();
        var generatedAt1 = data1.GeneratedAt;

        // Wait a tiny bit
        await Task.Delay(10);

        // Second call should return cached data with same timestamp
        var data2 = await _service.GetPricingPageDataAsync();

        Assert.Equal(generatedAt1, data2.GeneratedAt);
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task InvalidateCache_ClearsCachedData()
    {
        // First call to populate cache
        await _service.GetActivePricingTiersAsync();

        // Modify data
        var tier = await _context.PricingTiers.FindAsync(1);
        tier!.Name = "Modified Craft";
        await _context.SaveChangesAsync();

        // Invalidate cache
        _service.InvalidateCache();

        // Next call should get fresh data
        var tiers = (await _service.GetActivePricingTiersAsync()).ToList();
        var craftTier = tiers.First(t => t.Slug == "craft");

        Assert.Equal("Modified Craft", craftTier.Name);
    }

    #endregion
}
