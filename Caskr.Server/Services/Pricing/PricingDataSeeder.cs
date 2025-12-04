using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services.Pricing;

/// <summary>
/// Seeds initial pricing data for the US domestic market launch.
/// Pricing: Starter $699/mo, Growth $1,699/mo, Professional $2,999/mo, Enterprise custom.
/// </summary>
public class PricingDataSeeder
{
    private readonly CaskrDbContext _context;
    private readonly ILogger<PricingDataSeeder> _logger;

    public PricingDataSeeder(CaskrDbContext context, ILogger<PricingDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all pricing data if not already present.
    /// </summary>
    public async Task SeedAsync()
    {
        var hasExistingTiers = await _context.PricingTiers.AnyAsync();
        if (hasExistingTiers)
        {
            _logger.LogInformation("Pricing data already exists, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding pricing data for US domestic launch...");

        await SeedFeaturesAsync();
        await SeedTiersAsync();
        await SeedTierFeaturesAsync();
        await SeedFaqsAsync();

        await _context.SaveChangesAsync();
        _logger.LogInformation("Pricing data seeded successfully");
    }

    private async Task SeedFeaturesAsync()
    {
        var features = new List<PricingFeature>
        {
            // Core Features
            new() { Id = 1, Name = "Barrel Inventory", Description = "Complete barrel lifecycle tracking", Category = "Core", SortOrder = 1 },
            new() { Id = 2, Name = "Barrel Limit", Description = "Maximum barrels you can track", Category = "Core", SortOrder = 2 },
            new() { Id = 3, Name = "Users", Description = "Team member accounts", Category = "Core", SortOrder = 3 },
            new() { Id = 4, Name = "Locations", Description = "Warehouse/facility locations", Category = "Core", SortOrder = 4 },

            // TTB Compliance
            new() { Id = 5, Name = "TTB Compliance", Description = "Full TTB form automation (5110.28, 5110.40, 5100.16)", Category = "Compliance", SortOrder = 1 },
            new() { Id = 6, Name = "Gauge Records", Description = "Temperature-corrected proof gallon calculations", Category = "Compliance", SortOrder = 2 },
            new() { Id = 7, Name = "Excise Tax Calculation", Description = "Federal excise tax with reduced rate tracking", Category = "Compliance", SortOrder = 3 },
            new() { Id = 8, Name = "Audit Trail", Description = "31+ field comprehensive audit logging", Category = "Compliance", SortOrder = 4 },

            // Financial
            new() { Id = 9, Name = "QuickBooks Integration", Description = "Bi-directional sync with QuickBooks Online", Category = "Financial", SortOrder = 1 },
            new() { Id = 10, Name = "Invoice Sync", Description = "Automatic invoice creation in QuickBooks", Category = "Financial", SortOrder = 2 },
            new() { Id = 11, Name = "COGS Tracking", Description = "Cost of goods sold calculation and journal entries", Category = "Financial", SortOrder = 3 },

            // Reporting
            new() { Id = 12, Name = "Standard Reports", Description = "Pre-built financial, inventory, and compliance reports", Category = "Reporting", SortOrder = 1 },
            new() { Id = 13, Name = "Custom Report Builder", Description = "Drag-and-drop report creation with 20+ tables", Category = "Reporting", SortOrder = 2 },
            new() { Id = 14, Name = "Export Options", Description = "Export to CSV and PDF", Category = "Reporting", SortOrder = 3 },

            // Investor Portal
            new() { Id = 15, Name = "Investor Portal", Description = "Customer-facing portal for cask ownership", Category = "Portal", SortOrder = 1 },
            new() { Id = 16, Name = "Investor Limit", Description = "Maximum investor accounts", Category = "Portal", SortOrder = 2 },
            new() { Id = 17, Name = "Document Management", Description = "Ownership certificates, photos, invoices", Category = "Portal", SortOrder = 3 },
            new() { Id = 18, Name = "Maturation Tracking", Description = "Age and progress tracking for investors", Category = "Portal", SortOrder = 4 },

            // Mobile & Operations
            new() { Id = 19, Name = "Mobile Access", Description = "PWA mobile experience", Category = "Operations", SortOrder = 1 },
            new() { Id = 20, Name = "Barcode Scanning", Description = "Web-based QR and barcode scanning (5 formats)", Category = "Operations", SortOrder = 2 },
            new() { Id = 21, Name = "Offline Support", Description = "Work offline with automatic sync", Category = "Operations", SortOrder = 3 },

            // Integration
            new() { Id = 22, Name = "Webhooks", Description = "12 event types for integrations", Category = "Integration", SortOrder = 1 },
            new() { Id = 23, Name = "API Access", Description = "Full REST API with documentation", Category = "Integration", SortOrder = 2 },

            // Production (Coming Soon)
            new() { Id = 24, Name = "Production Planning", Description = "Scheduling and capacity management", Category = "Production", SortOrder = 1 },

            // Support
            new() { Id = 25, Name = "Support Response", Description = "Support response time SLA", Category = "Support", SortOrder = 1 },
            new() { Id = 26, Name = "Onboarding", Description = "Implementation assistance", Category = "Support", SortOrder = 2 },
            new() { Id = 27, Name = "Dedicated Account Manager", Description = "Named account manager", Category = "Support", SortOrder = 3 },
        };

        foreach (var feature in features)
        {
            feature.CreatedAt = DateTime.UtcNow;
            feature.UpdatedAt = DateTime.UtcNow;
            feature.IsActive = true;
        }

        await _context.PricingFeatures.AddRangeAsync(features);
    }

    private async Task SeedTiersAsync()
    {
        var tiers = new List<PricingTier>
        {
            new()
            {
                Id = 1,
                Name = "Starter",
                Slug = "starter",
                Tagline = "For emerging craft distilleries",
                MonthlyPriceCents = 69900, // $699/mo
                AnnualPriceCents = 671040, // $5,592/yr ($559/mo - 20% off)
                AnnualDiscountPercent = 20,
                IsPopular = false,
                IsCustomPricing = false,
                CtaText = "Start Free Trial",
                CtaUrl = "/signup?plan=starter",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Name = "Growth",
                Slug = "growth",
                Tagline = "For growing craft distilleries",
                MonthlyPriceCents = 169900, // $1,699/mo
                AnnualPriceCents = 1631040, // $13,592/yr ($1,359/mo - 20% off)
                AnnualDiscountPercent = 20,
                IsPopular = true,
                IsCustomPricing = false,
                CtaText = "Start Free Trial",
                CtaUrl = "/signup?plan=growth",
                SortOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 3,
                Name = "Professional",
                Slug = "professional",
                Tagline = "For established multi-location distilleries",
                MonthlyPriceCents = 299900, // $2,999/mo
                AnnualPriceCents = 2879040, // $23,992/yr ($1,999/mo - 20% off) - corrected to $2,399/mo
                AnnualDiscountPercent = 20,
                IsPopular = false,
                IsCustomPricing = false,
                CtaText = "Start Free Trial",
                CtaUrl = "/signup?plan=professional",
                SortOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 4,
                Name = "Enterprise",
                Slug = "enterprise",
                Tagline = "Custom solutions for large operations",
                MonthlyPriceCents = null,
                AnnualPriceCents = null,
                AnnualDiscountPercent = 0,
                IsPopular = false,
                IsCustomPricing = true,
                CtaText = "Contact Sales",
                CtaUrl = "/contact?plan=enterprise",
                SortOrder = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        await _context.PricingTiers.AddRangeAsync(tiers);
    }

    private async Task SeedTierFeaturesAsync()
    {
        var tierFeatures = new List<PricingTierFeature>
        {
            // Starter Tier (ID: 1)
            // Core
            new() { TierId = 1, FeatureId = 1, IsIncluded = true },
            new() { TierId = 1, FeatureId = 2, IsIncluded = true, LimitValue = "500", LimitDescription = "barrels" },
            new() { TierId = 1, FeatureId = 3, IsIncluded = true, LimitValue = "3", LimitDescription = "users" },
            new() { TierId = 1, FeatureId = 4, IsIncluded = true, LimitValue = "1", LimitDescription = "location" },
            // Compliance
            new() { TierId = 1, FeatureId = 5, IsIncluded = true },
            new() { TierId = 1, FeatureId = 6, IsIncluded = true },
            new() { TierId = 1, FeatureId = 7, IsIncluded = true },
            new() { TierId = 1, FeatureId = 8, IsIncluded = true },
            // Financial
            new() { TierId = 1, FeatureId = 9, IsIncluded = true },
            new() { TierId = 1, FeatureId = 10, IsIncluded = true },
            new() { TierId = 1, FeatureId = 11, IsIncluded = true },
            // Reporting
            new() { TierId = 1, FeatureId = 12, IsIncluded = true, LimitValue = "15", LimitDescription = "reports" },
            new() { TierId = 1, FeatureId = 13, IsIncluded = false },
            new() { TierId = 1, FeatureId = 14, IsIncluded = true },
            // Portal
            new() { TierId = 1, FeatureId = 15, IsIncluded = false },
            new() { TierId = 1, FeatureId = 16, IsIncluded = false },
            new() { TierId = 1, FeatureId = 17, IsIncluded = false },
            new() { TierId = 1, FeatureId = 18, IsIncluded = false },
            // Operations
            new() { TierId = 1, FeatureId = 19, IsIncluded = true },
            new() { TierId = 1, FeatureId = 20, IsIncluded = true },
            new() { TierId = 1, FeatureId = 21, IsIncluded = true },
            // Integration
            new() { TierId = 1, FeatureId = 22, IsIncluded = false },
            new() { TierId = 1, FeatureId = 23, IsIncluded = false },
            // Production
            new() { TierId = 1, FeatureId = 24, IsIncluded = false },
            // Support
            new() { TierId = 1, FeatureId = 25, IsIncluded = true, LimitValue = "48h", LimitDescription = "response" },
            new() { TierId = 1, FeatureId = 26, IsIncluded = true, LimitValue = "Self-serve" },
            new() { TierId = 1, FeatureId = 27, IsIncluded = false },

            // Growth Tier (ID: 2)
            // Core
            new() { TierId = 2, FeatureId = 1, IsIncluded = true },
            new() { TierId = 2, FeatureId = 2, IsIncluded = true, LimitValue = "2,500", LimitDescription = "barrels" },
            new() { TierId = 2, FeatureId = 3, IsIncluded = true, LimitValue = "10", LimitDescription = "users" },
            new() { TierId = 2, FeatureId = 4, IsIncluded = true, LimitValue = "2", LimitDescription = "locations" },
            // Compliance
            new() { TierId = 2, FeatureId = 5, IsIncluded = true },
            new() { TierId = 2, FeatureId = 6, IsIncluded = true },
            new() { TierId = 2, FeatureId = 7, IsIncluded = true },
            new() { TierId = 2, FeatureId = 8, IsIncluded = true },
            // Financial
            new() { TierId = 2, FeatureId = 9, IsIncluded = true },
            new() { TierId = 2, FeatureId = 10, IsIncluded = true },
            new() { TierId = 2, FeatureId = 11, IsIncluded = true },
            // Reporting
            new() { TierId = 2, FeatureId = 12, IsIncluded = true, LimitValue = "30+", LimitDescription = "reports" },
            new() { TierId = 2, FeatureId = 13, IsIncluded = true },
            new() { TierId = 2, FeatureId = 14, IsIncluded = true },
            // Portal
            new() { TierId = 2, FeatureId = 15, IsIncluded = true },
            new() { TierId = 2, FeatureId = 16, IsIncluded = true, LimitValue = "50", LimitDescription = "investors" },
            new() { TierId = 2, FeatureId = 17, IsIncluded = true },
            new() { TierId = 2, FeatureId = 18, IsIncluded = true },
            // Operations
            new() { TierId = 2, FeatureId = 19, IsIncluded = true },
            new() { TierId = 2, FeatureId = 20, IsIncluded = true },
            new() { TierId = 2, FeatureId = 21, IsIncluded = true },
            // Integration
            new() { TierId = 2, FeatureId = 22, IsIncluded = true },
            new() { TierId = 2, FeatureId = 23, IsIncluded = true },
            // Production
            new() { TierId = 2, FeatureId = 24, IsIncluded = false },
            // Support
            new() { TierId = 2, FeatureId = 25, IsIncluded = true, LimitValue = "24h", LimitDescription = "response" },
            new() { TierId = 2, FeatureId = 26, IsIncluded = true, LimitValue = "Guided (2h)" },
            new() { TierId = 2, FeatureId = 27, IsIncluded = false },

            // Professional Tier (ID: 3)
            // Core
            new() { TierId = 3, FeatureId = 1, IsIncluded = true },
            new() { TierId = 3, FeatureId = 2, IsIncluded = true, LimitValue = "10,000", LimitDescription = "barrels" },
            new() { TierId = 3, FeatureId = 3, IsIncluded = true, LimitValue = "25", LimitDescription = "users" },
            new() { TierId = 3, FeatureId = 4, IsIncluded = true, LimitValue = "5", LimitDescription = "locations" },
            // Compliance
            new() { TierId = 3, FeatureId = 5, IsIncluded = true },
            new() { TierId = 3, FeatureId = 6, IsIncluded = true },
            new() { TierId = 3, FeatureId = 7, IsIncluded = true },
            new() { TierId = 3, FeatureId = 8, IsIncluded = true },
            // Financial
            new() { TierId = 3, FeatureId = 9, IsIncluded = true },
            new() { TierId = 3, FeatureId = 10, IsIncluded = true },
            new() { TierId = 3, FeatureId = 11, IsIncluded = true },
            // Reporting
            new() { TierId = 3, FeatureId = 12, IsIncluded = true, LimitValue = "30+", LimitDescription = "reports" },
            new() { TierId = 3, FeatureId = 13, IsIncluded = true },
            new() { TierId = 3, FeatureId = 14, IsIncluded = true },
            // Portal
            new() { TierId = 3, FeatureId = 15, IsIncluded = true },
            new() { TierId = 3, FeatureId = 16, IsIncluded = true, LimitValue = "200", LimitDescription = "investors" },
            new() { TierId = 3, FeatureId = 17, IsIncluded = true },
            new() { TierId = 3, FeatureId = 18, IsIncluded = true },
            // Operations
            new() { TierId = 3, FeatureId = 19, IsIncluded = true },
            new() { TierId = 3, FeatureId = 20, IsIncluded = true },
            new() { TierId = 3, FeatureId = 21, IsIncluded = true },
            // Integration
            new() { TierId = 3, FeatureId = 22, IsIncluded = true },
            new() { TierId = 3, FeatureId = 23, IsIncluded = true },
            // Production
            new() { TierId = 3, FeatureId = 24, IsIncluded = true },
            // Support
            new() { TierId = 3, FeatureId = 25, IsIncluded = true, LimitValue = "4h", LimitDescription = "response" },
            new() { TierId = 3, FeatureId = 26, IsIncluded = true, LimitValue = "White-glove (8h)" },
            new() { TierId = 3, FeatureId = 27, IsIncluded = false },

            // Enterprise Tier (ID: 4)
            // Core
            new() { TierId = 4, FeatureId = 1, IsIncluded = true },
            new() { TierId = 4, FeatureId = 2, IsIncluded = true, LimitValue = "Unlimited" },
            new() { TierId = 4, FeatureId = 3, IsIncluded = true, LimitValue = "Unlimited" },
            new() { TierId = 4, FeatureId = 4, IsIncluded = true, LimitValue = "Unlimited" },
            // Compliance
            new() { TierId = 4, FeatureId = 5, IsIncluded = true },
            new() { TierId = 4, FeatureId = 6, IsIncluded = true },
            new() { TierId = 4, FeatureId = 7, IsIncluded = true },
            new() { TierId = 4, FeatureId = 8, IsIncluded = true },
            // Financial
            new() { TierId = 4, FeatureId = 9, IsIncluded = true },
            new() { TierId = 4, FeatureId = 10, IsIncluded = true },
            new() { TierId = 4, FeatureId = 11, IsIncluded = true },
            // Reporting
            new() { TierId = 4, FeatureId = 12, IsIncluded = true, LimitValue = "30+", LimitDescription = "reports" },
            new() { TierId = 4, FeatureId = 13, IsIncluded = true },
            new() { TierId = 4, FeatureId = 14, IsIncluded = true },
            // Portal
            new() { TierId = 4, FeatureId = 15, IsIncluded = true },
            new() { TierId = 4, FeatureId = 16, IsIncluded = true, LimitValue = "Unlimited" },
            new() { TierId = 4, FeatureId = 17, IsIncluded = true },
            new() { TierId = 4, FeatureId = 18, IsIncluded = true },
            // Operations
            new() { TierId = 4, FeatureId = 19, IsIncluded = true },
            new() { TierId = 4, FeatureId = 20, IsIncluded = true },
            new() { TierId = 4, FeatureId = 21, IsIncluded = true },
            // Integration
            new() { TierId = 4, FeatureId = 22, IsIncluded = true },
            new() { TierId = 4, FeatureId = 23, IsIncluded = true },
            // Production
            new() { TierId = 4, FeatureId = 24, IsIncluded = true },
            // Support
            new() { TierId = 4, FeatureId = 25, IsIncluded = true, LimitValue = "1h", LimitDescription = "response" },
            new() { TierId = 4, FeatureId = 26, IsIncluded = true, LimitValue = "Custom" },
            new() { TierId = 4, FeatureId = 27, IsIncluded = true },
        };

        await _context.PricingTierFeatures.AddRangeAsync(tierFeatures);
    }

    private async Task SeedFaqsAsync()
    {
        var faqs = new List<PricingFaq>
        {
            new()
            {
                Id = 1,
                Question = "What is included in the free trial?",
                Answer = "All features of the **Growth tier** are included in our **14-day free trial**. No credit card required. You'll have full access to TTB compliance, QuickBooks integration, custom reporting, and the investor portal.",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Question = "How does TTB compliance automation work?",
                Answer = "Caskr automatically generates **Forms 5110.28** (Processing Operations) and **5110.40** (Storage Operations) based on your barrel transactions. Temperature-corrected proof gallon calculations, gauge records, and excise tax calculations are all handled automatically with a comprehensive audit trail.",
                SortOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 3,
                Question = "Is there a discount for annual billing?",
                Answer = "Yes, annual billing saves you **20%** compared to monthly billing. This is automatically applied when you select annual billing during signup.",
                SortOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 4,
                Question = "Can I upgrade or downgrade my plan?",
                Answer = "Yes, you can change your plan at any time. Upgrades take effect immediately with prorated billing. Downgrades take effect at the start of your next billing cycle.",
                SortOrder = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 5,
                Question = "How does the QuickBooks integration work?",
                Answer = "Caskr connects to **QuickBooks Online** via OAuth 2.0. Invoices sync bi-directionally, and COGS journal entries are created automatically when batches complete. We support 8 account types and provide real-time sync status monitoring.",
                SortOrder = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 6,
                Question = "What is the investor portal?",
                Answer = "The investor portal allows your cask ownership program participants to log in and view their barrel investments. They can see maturation progress, download ownership certificates, view photos, and track their cask's journey from fill to bottle.",
                SortOrder = 6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 7,
                Question = "Do you offer discounts for early adopters?",
                Answer = "Yes! Our first 25 customers receive **25% off** their first year. Contact sales with code **EARLYBIRD25** to claim this offer.",
                SortOrder = 7,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 8,
                Question = "How long does implementation take?",
                Answer = "Most distilleries are up and running within **2-4 weeks**. Self-serve onboarding takes about a day for basic setup. Guided onboarding (Growth tier) includes 2 hours of implementation support. White-glove onboarding (Professional tier) includes 8 hours of hands-on assistance.",
                SortOrder = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        await _context.PricingFaqs.AddRangeAsync(faqs);
    }
}
