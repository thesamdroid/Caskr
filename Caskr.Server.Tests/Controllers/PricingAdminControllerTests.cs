using System.Security.Claims;
using Caskr.server.Controllers.Admin;
using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Caskr.server.Services;
using Caskr.server.Services.Pricing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Controllers;

/// <summary>
/// Tests for the PricingAdminController which handles CRUD operations
/// for pricing tiers, features, FAQs, and promotions.
/// </summary>
public class PricingAdminControllerTests : IDisposable
{
    private readonly CaskrDbContext _context;
    private readonly Mock<IPricingAuditLogger> _auditLoggerMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly Mock<IUsersService> _usersServiceMock;
    private readonly Mock<ILogger<PricingAdminController>> _loggerMock;
    private readonly PricingAdminController _controller;

    public PricingAdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
        _auditLoggerMock = new Mock<IPricingAuditLogger>();
        _pricingServiceMock = new Mock<IPricingService>();
        _usersServiceMock = new Mock<IUsersService>();
        _loggerMock = new Mock<ILogger<PricingAdminController>>();

        _controller = new PricingAdminController(
            _context,
            _auditLoggerMock.Object,
            _pricingServiceMock.Object,
            _usersServiceMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tier = new PricingTier
        {
            Id = 1,
            Name = "Craft",
            Slug = "craft",
            Tagline = "Perfect for craft distilleries",
            MonthlyPriceCents = 29900,
            AnnualPriceCents = 287040,
            AnnualDiscountPercent = 20,
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var feature = new PricingFeature
        {
            Id = 1,
            Name = "TTB Compliance",
            Description = "Automated TTB compliance",
            Category = "Compliance",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var faq = new PricingFaq
        {
            Id = 1,
            Question = "What is included?",
            Answer = "Full access to all features.",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion = new PricingPromotion
        {
            Id = 1,
            Code = "WELCOME20",
            Description = "20% off first year",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PricingTiers.Add(tier);
        _context.PricingFeatures.Add(feature);
        _context.PricingFaqs.Add(faq);
        _context.PricingPromotions.Add(promotion);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SetupAdminUser(int userId = 1)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var adminUser = new User
        {
            Id = userId,
            Name = "Admin User",
            Email = "admin@test.com",
            UserTypeId = (int)Caskr.server.UserType.Admin,
            IsActive = true
        };

        _usersServiceMock.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(adminUser);
    }

    private void SetupNonAdminUser(int userId = 2)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var regularUser = new User
        {
            Id = userId,
            Name = "Regular User",
            Email = "user@test.com",
            UserTypeId = (int)Caskr.server.UserType.Distiller,
            IsActive = true
        };

        _usersServiceMock.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(regularUser);
    }

    #region Authorization Tests

    [Fact]
    public async Task GetAllTiers_ReturnsForbidForNonAdmin()
    {
        SetupNonAdminUser();

        var result = await _controller.GetAllTiers();

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetAllTiers_ReturnsOkForAdmin()
    {
        SetupAdminUser();

        var result = await _controller.GetAllTiers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tiers = Assert.IsAssignableFrom<IEnumerable<PricingTier>>(okResult.Value);
        Assert.Single(tiers);
    }

    #endregion

    #region Tier CRUD Tests

    [Fact]
    public async Task GetTier_ReturnsNotFoundForInvalidId()
    {
        SetupAdminUser();

        var result = await _controller.GetTier(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetTier_ReturnsTierForValidId()
    {
        SetupAdminUser();

        var result = await _controller.GetTier(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tier = Assert.IsType<PricingTier>(okResult.Value);
        Assert.Equal("Craft", tier.Name);
    }

    [Fact]
    public async Task CreateTier_ReturnsBadRequestForMissingName()
    {
        SetupAdminUser();

        var newTier = new PricingTier { Slug = "new-tier" };

        var result = await _controller.CreateTier(newTier);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTier_ReturnsBadRequestForDuplicateSlug()
    {
        SetupAdminUser();

        var newTier = new PricingTier { Name = "New Tier", Slug = "craft" };

        var result = await _controller.CreateTier(newTier);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTier_ReturnsCreatedForValidTier()
    {
        SetupAdminUser();

        var newTier = new PricingTier
        {
            Name = "Professional",
            Slug = "professional",
            MonthlyPriceCents = 49900,
            IsActive = true
        };

        var result = await _controller.CreateTier(newTier);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var tier = Assert.IsType<PricingTier>(createdResult.Value);
        Assert.Equal("Professional", tier.Name);

        _auditLoggerMock.Verify(
            a => a.LogChangeAsync(PricingAuditAction.Create, It.IsAny<PricingTier>(), null, 1),
            Times.Once);
        _pricingServiceMock.Verify(s => s.InvalidateCache(), Times.Once);
    }

    [Fact]
    public async Task UpdateTier_ReturnsBadRequestForIdMismatch()
    {
        SetupAdminUser();

        var tier = new PricingTier { Id = 2, Name = "Updated", Slug = "updated" };

        var result = await _controller.UpdateTier(1, tier);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTier_ReturnsNotFoundForInvalidId()
    {
        SetupAdminUser();

        var tier = new PricingTier { Id = 999, Name = "Updated", Slug = "updated" };

        var result = await _controller.UpdateTier(999, tier);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTier_ReturnsOkForValidUpdate()
    {
        SetupAdminUser();

        var tier = new PricingTier
        {
            Id = 1,
            Name = "Updated Craft",
            Slug = "craft",
            MonthlyPriceCents = 39900,
            IsActive = true
        };

        var result = await _controller.UpdateTier(1, tier);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedTier = Assert.IsType<PricingTier>(okResult.Value);
        Assert.Equal("Updated Craft", updatedTier.Name);
        Assert.Equal(39900, updatedTier.MonthlyPriceCents);

        _pricingServiceMock.Verify(s => s.InvalidateCache(), Times.Once);
    }

    [Fact]
    public async Task DeleteTier_ReturnsNotFoundForInvalidId()
    {
        SetupAdminUser();

        var result = await _controller.DeleteTier(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteTier_ReturnsNoContentForValidDelete()
    {
        SetupAdminUser();

        var result = await _controller.DeleteTier(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await _context.PricingTiers.FindAsync(1));

        _pricingServiceMock.Verify(s => s.InvalidateCache(), Times.Once);
    }

    #endregion

    #region Feature CRUD Tests

    [Fact]
    public async Task GetAllFeatures_ReturnsFeatures()
    {
        SetupAdminUser();

        var result = await _controller.GetAllFeatures();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var features = Assert.IsAssignableFrom<IEnumerable<PricingFeature>>(okResult.Value);
        Assert.Single(features);
    }

    [Fact]
    public async Task CreateFeature_ReturnsBadRequestForMissingName()
    {
        SetupAdminUser();

        var feature = new PricingFeature { Description = "Test" };

        var result = await _controller.CreateFeature(feature);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateFeature_ReturnsCreatedForValidFeature()
    {
        SetupAdminUser();

        var feature = new PricingFeature
        {
            Name = "New Feature",
            Description = "A new feature",
            Category = "Operations",
            IsActive = true
        };

        var result = await _controller.CreateFeature(feature);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdFeature = Assert.IsType<PricingFeature>(createdResult.Value);
        Assert.Equal("New Feature", createdFeature.Name);

        _pricingServiceMock.Verify(s => s.InvalidateCache(), Times.Once);
    }

    [Fact]
    public async Task UpdateFeature_ReturnsOkForValidUpdate()
    {
        SetupAdminUser();

        var feature = new PricingFeature
        {
            Id = 1,
            Name = "Updated Feature",
            Description = "Updated description",
            Category = "Updated",
            IsActive = true
        };

        var result = await _controller.UpdateFeature(1, feature);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedFeature = Assert.IsType<PricingFeature>(okResult.Value);
        Assert.Equal("Updated Feature", updatedFeature.Name);
    }

    [Fact]
    public async Task DeleteFeature_ReturnsNoContentForValidDelete()
    {
        SetupAdminUser();

        var result = await _controller.DeleteFeature(1);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region FAQ CRUD Tests

    [Fact]
    public async Task GetAllFaqs_ReturnsFaqs()
    {
        SetupAdminUser();

        var result = await _controller.GetAllFaqs();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var faqs = Assert.IsAssignableFrom<IEnumerable<PricingFaq>>(okResult.Value);
        Assert.Single(faqs);
    }

    [Fact]
    public async Task CreateFaq_ReturnsBadRequestForMissingQuestion()
    {
        SetupAdminUser();

        var faq = new PricingFaq { Answer = "An answer" };

        var result = await _controller.CreateFaq(faq);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateFaq_ReturnsCreatedForValidFaq()
    {
        SetupAdminUser();

        var faq = new PricingFaq
        {
            Question = "New Question?",
            Answer = "New Answer",
            SortOrder = 2,
            IsActive = true
        };

        var result = await _controller.CreateFaq(faq);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdFaq = Assert.IsType<PricingFaq>(createdResult.Value);
        Assert.Equal("New Question?", createdFaq.Question);
    }

    [Fact]
    public async Task UpdateFaq_ReturnsOkForValidUpdate()
    {
        SetupAdminUser();

        var faq = new PricingFaq
        {
            Id = 1,
            Question = "Updated Question?",
            Answer = "Updated Answer",
            IsActive = true
        };

        var result = await _controller.UpdateFaq(1, faq);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedFaq = Assert.IsType<PricingFaq>(okResult.Value);
        Assert.Equal("Updated Question?", updatedFaq.Question);
    }

    [Fact]
    public async Task DeleteFaq_ReturnsNoContentForValidDelete()
    {
        SetupAdminUser();

        var result = await _controller.DeleteFaq(1);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Promotion CRUD Tests

    [Fact]
    public async Task GetAllPromotions_ReturnsPromotions()
    {
        SetupAdminUser();

        var result = await _controller.GetAllPromotions();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var promotions = Assert.IsAssignableFrom<IEnumerable<PricingPromotion>>(okResult.Value);
        Assert.Single(promotions);
    }

    [Fact]
    public async Task CreatePromotion_ReturnsBadRequestForMissingCode()
    {
        SetupAdminUser();

        var promo = new PricingPromotion { Description = "Test" };

        var result = await _controller.CreatePromotion(promo);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePromotion_ReturnsBadRequestForDuplicateCode()
    {
        SetupAdminUser();

        var promo = new PricingPromotion { Code = "WELCOME20", Description = "Duplicate" };

        var result = await _controller.CreatePromotion(promo);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePromotion_ReturnsCreatedForValidPromotion()
    {
        SetupAdminUser();

        var promo = new PricingPromotion
        {
            Code = "NEWPROMO",
            Description = "New promo",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5000,
            IsActive = true
        };

        var result = await _controller.CreatePromotion(promo);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdPromo = Assert.IsType<PricingPromotion>(createdResult.Value);
        Assert.Equal("NEWPROMO", createdPromo.Code);
        Assert.Equal(0, createdPromo.CurrentRedemptions);
    }

    [Fact]
    public async Task UpdatePromotion_ReturnsOkForValidUpdate()
    {
        SetupAdminUser();

        var promo = new PricingPromotion
        {
            Id = 1,
            Code = "WELCOME20",
            Description = "Updated description",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 25,
            IsActive = true
        };

        var result = await _controller.UpdatePromotion(1, promo);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedPromo = Assert.IsType<PricingPromotion>(okResult.Value);
        Assert.Equal(25, updatedPromo.DiscountValue);
    }

    [Fact]
    public async Task DeletePromotion_ReturnsNoContentForValidDelete()
    {
        SetupAdminUser();

        var result = await _controller.DeletePromotion(1);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Tier Features Tests

    [Fact]
    public async Task AddFeatureToTier_ReturnsNotFoundForInvalidTier()
    {
        SetupAdminUser();

        var tierFeature = new PricingTierFeature { FeatureId = 1, IsIncluded = true };

        var result = await _controller.AddFeatureToTier(999, tierFeature);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("Tier not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task AddFeatureToTier_ReturnsNotFoundForInvalidFeature()
    {
        SetupAdminUser();

        var tierFeature = new PricingTierFeature { FeatureId = 999, IsIncluded = true };

        var result = await _controller.AddFeatureToTier(1, tierFeature);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("Feature not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task AddFeatureToTier_ReturnsCreatedForValidAssociation()
    {
        SetupAdminUser();

        var tierFeature = new PricingTierFeature
        {
            FeatureId = 1,
            IsIncluded = true,
            LimitValue = "Unlimited"
        };

        var result = await _controller.AddFeatureToTier(1, tierFeature);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var created = Assert.IsType<PricingTierFeature>(createdResult.Value);
        Assert.True(created.IsIncluded);
        Assert.Equal("Unlimited", created.LimitValue);
    }

    [Fact]
    public async Task AddFeatureToTier_ReturnsBadRequestForDuplicate()
    {
        SetupAdminUser();

        // First add
        var tierFeature = new PricingTierFeature { FeatureId = 1, IsIncluded = true };
        await _controller.AddFeatureToTier(1, tierFeature);

        // Try to add again
        var duplicateTierFeature = new PricingTierFeature { FeatureId = 1, IsIncluded = false };
        var result = await _controller.AddFeatureToTier(1, duplicateTierFeature);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTierFeature_ReturnsOkForValidUpdate()
    {
        SetupAdminUser();

        // First add
        var tierFeature = new PricingTierFeature { FeatureId = 1, IsIncluded = true };
        await _controller.AddFeatureToTier(1, tierFeature);

        // Update
        var updateTierFeature = new PricingTierFeature
        {
            IsIncluded = false,
            LimitValue = "Limited",
            LimitDescription = "Up to 100"
        };

        var result = await _controller.UpdateTierFeature(1, 1, updateTierFeature);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<PricingTierFeature>(okResult.Value);
        Assert.False(updated.IsIncluded);
        Assert.Equal("Limited", updated.LimitValue);
    }

    [Fact]
    public async Task RemoveFeatureFromTier_ReturnsNoContentForValidRemoval()
    {
        SetupAdminUser();

        // First add
        var tierFeature = new PricingTierFeature { FeatureId = 1, IsIncluded = true };
        await _controller.AddFeatureToTier(1, tierFeature);

        // Remove
        var result = await _controller.RemoveFeatureFromTier(1, 1);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Audit Log Tests

    [Fact]
    public async Task GetAuditLogs_ReturnsLogsFromService()
    {
        SetupAdminUser();

        var expectedLogs = new List<PricingAuditLog>
        {
            new()
            {
                Id = 1,
                EntityType = "PricingTier",
                Action = PricingAuditAction.Create,
                ChangeTimestamp = DateTime.UtcNow
            }
        };

        _auditLoggerMock.Setup(a => a.GetAuditLogsAsync(null, null, null, null, 100))
            .ReturnsAsync(expectedLogs);

        var result = await _controller.GetAuditLogs();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<IEnumerable<PricingAuditLog>>(okResult.Value);
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetAuditLogs_PassesFilterParameters()
    {
        SetupAdminUser();

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        _auditLoggerMock.Setup(a => a.GetAuditLogsAsync("PricingTier", 1, startDate, endDate, 50))
            .ReturnsAsync(new List<PricingAuditLog>());

        await _controller.GetAuditLogs("PricingTier", 1, startDate, endDate, 50);

        _auditLoggerMock.Verify(
            a => a.GetAuditLogsAsync("PricingTier", 1, startDate, endDate, 50),
            Times.Once);
    }

    #endregion

    #region Preview Tests

    [Fact]
    public async Task GetPreview_ReturnsCompleteData()
    {
        SetupAdminUser();

        var result = await _controller.GetPreview();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PricingPageDataDto>(okResult.Value);
        Assert.Single(data.Tiers);
        Assert.Single(data.FeaturesByCategory);
        Assert.Single(data.Faqs);
    }

    #endregion

    #region SuperAdmin Tests

    [Fact]
    public async Task GetAllTiers_ReturnsOkForSuperAdmin()
    {
        // Setup SuperAdmin user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var superAdmin = new User
        {
            Id = 1,
            Name = "Super Admin",
            Email = "superadmin@test.com",
            UserTypeId = (int)Caskr.server.UserType.SuperAdmin,
            IsActive = true
        };

        _usersServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(superAdmin);

        var result = await _controller.GetAllTiers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
