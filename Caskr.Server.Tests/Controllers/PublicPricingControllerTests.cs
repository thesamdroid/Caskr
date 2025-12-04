using Caskr.server.Controllers;
using Caskr.server.Models.Pricing;
using Caskr.server.Services.Pricing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Controllers;

public class PublicPricingControllerTests : IDisposable
{
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<PublicPricingController>> _loggerMock;
    private readonly PublicPricingController _controller;

    public PublicPricingControllerTests()
    {
        _pricingServiceMock = new Mock<IPricingService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<PublicPricingController>>();

        _controller = new PublicPricingController(
            _pricingServiceMock.Object,
            _cache,
            _loggerMock.Object);

        // Set up HttpContext for rate limiting
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    #region GetPricingPageData Tests

    [Fact]
    public async Task GetPricingPageData_ReturnsOkWithData()
    {
        var expectedData = new PricingPageDataDto
        {
            Tiers = [new PricingTierDto { Name = "Craft", Slug = "craft" }],
            FeaturesByCategory = [new PricingFeatureCategoryDto { Category = "Compliance" }],
            Faqs = [new PricingFaqDto { Question = "Test?" }],
            GeneratedAt = DateTime.UtcNow
        };

        _pricingServiceMock.Setup(s => s.GetPricingPageDataAsync())
            .ReturnsAsync(expectedData);

        var result = await _controller.GetPricingPageData();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PricingPageDataDto>(okResult.Value);
        Assert.Single(data.Tiers);
    }

    #endregion

    #region GetTiers Tests

    [Fact]
    public async Task GetTiers_ReturnsOkWithTiers()
    {
        var expectedTiers = new List<PricingTierDto>
        {
            new() { Id = 1, Name = "Craft", Slug = "craft" },
            new() { Id = 2, Name = "Growth", Slug = "growth" }
        };

        _pricingServiceMock.Setup(s => s.GetActivePricingTiersAsync())
            .ReturnsAsync(expectedTiers);

        var result = await _controller.GetTiers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tiers = Assert.IsType<List<PricingTierDto>>(okResult.Value);
        Assert.Equal(2, tiers.Count);
    }

    #endregion

    #region GetTierBySlug Tests

    [Fact]
    public async Task GetTierBySlug_ReturnsOkForValidSlug()
    {
        var expectedTier = new PricingTierDto { Id = 1, Name = "Craft", Slug = "craft" };

        _pricingServiceMock.Setup(s => s.GetTierBySlugAsync("craft"))
            .ReturnsAsync(expectedTier);

        var result = await _controller.GetTierBySlug("craft");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tier = Assert.IsType<PricingTierDto>(okResult.Value);
        Assert.Equal("Craft", tier.Name);
    }

    [Fact]
    public async Task GetTierBySlug_ReturnsNotFoundForInvalidSlug()
    {
        _pricingServiceMock.Setup(s => s.GetTierBySlugAsync("nonexistent"))
            .ReturnsAsync((PricingTierDto?)null);

        var result = await _controller.GetTierBySlug("nonexistent");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region GetFeatures Tests

    [Fact]
    public async Task GetFeatures_ReturnsOkWithFeatures()
    {
        var expectedFeatures = new List<PricingFeatureCategoryDto>
        {
            new() { Category = "Compliance", Features = [new PricingFeatureDto { Name = "TTB Compliance" }] }
        };

        _pricingServiceMock.Setup(s => s.GetPricingFeaturesAsync())
            .ReturnsAsync(expectedFeatures);

        var result = await _controller.GetFeatures();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var features = Assert.IsType<List<PricingFeatureCategoryDto>>(okResult.Value);
        Assert.Single(features);
    }

    #endregion

    #region GetFaqs Tests

    [Fact]
    public async Task GetFaqs_ReturnsOkWithFaqs()
    {
        var expectedFaqs = new List<PricingFaqDto>
        {
            new() { Id = 1, Question = "What is included?", Answer = "Everything." }
        };

        _pricingServiceMock.Setup(s => s.GetPricingFaqsAsync())
            .ReturnsAsync(expectedFaqs);

        var result = await _controller.GetFaqs();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var faqs = Assert.IsType<List<PricingFaqDto>>(okResult.Value);
        Assert.Single(faqs);
    }

    #endregion

    #region ValidatePromoCode Tests

    [Fact]
    public async Task ValidatePromoCode_ReturnsOkForValidCode()
    {
        var validationResult = new PromoCodeValidationResult
        {
            IsValid = true,
            Code = "WELCOME20",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20
        };

        _pricingServiceMock.Setup(s => s.ValidatePromoCodeAsync("WELCOME20", null))
            .ReturnsAsync(validationResult);

        var request = new ValidatePromoCodeRequest { Code = "WELCOME20" };
        var result = await _controller.ValidatePromoCode(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var validation = Assert.IsType<PromoCodeValidationResult>(okResult.Value);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task ValidatePromoCode_ReturnsBadRequestForEmptyCode()
    {
        var request = new ValidatePromoCodeRequest { Code = "" };
        var result = await _controller.ValidatePromoCode(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ValidatePromoCode_ReturnsOkWithInvalidResultForBadCode()
    {
        var validationResult = new PromoCodeValidationResult
        {
            IsValid = false,
            ErrorMessage = "Invalid promo code"
        };

        _pricingServiceMock.Setup(s => s.ValidatePromoCodeAsync("INVALID", null))
            .ReturnsAsync(validationResult);

        var request = new ValidatePromoCodeRequest { Code = "INVALID" };
        var result = await _controller.ValidatePromoCode(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var validation = Assert.IsType<PromoCodeValidationResult>(okResult.Value);
        Assert.False(validation.IsValid);
    }

    #endregion

    #region ApplyPromoCode Tests

    [Fact]
    public async Task ApplyPromoCode_ReturnsOkForSuccessfulApplication()
    {
        var applicationResult = new PromoCodeApplicationResult
        {
            Success = true,
            Code = "WELCOME20",
            TierId = 1,
            OriginalMonthlyPriceCents = 29900,
            DiscountedMonthlyPriceCents = 23920
        };

        _pricingServiceMock.Setup(s => s.ApplyPromoCodeAsync("WELCOME20", 1))
            .ReturnsAsync(applicationResult);

        var request = new ApplyPromoCodeRequest { Code = "WELCOME20", TierId = 1 };
        var result = await _controller.ApplyPromoCode(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var application = Assert.IsType<PromoCodeApplicationResult>(okResult.Value);
        Assert.True(application.Success);
        Assert.Equal(23920, application.DiscountedMonthlyPriceCents);
    }

    [Fact]
    public async Task ApplyPromoCode_ReturnsBadRequestForEmptyCode()
    {
        var request = new ApplyPromoCodeRequest { Code = "", TierId = 1 };
        var result = await _controller.ApplyPromoCode(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ApplyPromoCode_ReturnsBadRequestForInvalidTierId()
    {
        var request = new ApplyPromoCodeRequest { Code = "WELCOME20", TierId = 0 };
        var result = await _controller.ApplyPromoCode(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ApplyPromoCode_ReturnsBadRequestForFailedApplication()
    {
        var applicationResult = new PromoCodeApplicationResult
        {
            Success = false,
            ErrorMessage = "Tier not found"
        };

        _pricingServiceMock.Setup(s => s.ApplyPromoCodeAsync("WELCOME20", 999))
            .ReturnsAsync(applicationResult);

        var request = new ApplyPromoCodeRequest { Code = "WELCOME20", TierId = 999 };
        var result = await _controller.ApplyPromoCode(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task RateLimiting_Returns429WhenLimitExceeded()
    {
        _pricingServiceMock.Setup(s => s.GetActivePricingTiersAsync())
            .ReturnsAsync(new List<PricingTierDto>());

        // Make 100 requests (the limit)
        for (int i = 0; i < 100; i++)
        {
            await _controller.GetTiers();
        }

        // 101st request should be rate limited
        var result = await _controller.GetTiers();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(429, statusResult.StatusCode);
    }

    [Fact]
    public async Task RateLimiting_ReturnsConsistentPayloadAcrossEndpoints()
    {
        _pricingServiceMock.Setup(s => s.GetPricingPageDataAsync())
            .ReturnsAsync(new PricingPageDataDto
            {
                Tiers = new List<PricingTierDto>(),
                FeaturesByCategory = new List<PricingFeatureCategoryDto>(),
                Faqs = new List<PricingFaqDto>()
            });

        for (int i = 0; i < 100; i++)
        {
            await _controller.GetPricingPageData();
        }

        var result = await _controller.GetPricingPageData();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(429, statusResult.StatusCode);

        var messageProperty = statusResult.Value?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Rate limit exceeded. Please try again later.", messageProperty!.GetValue(statusResult.Value));
    }

    [Fact]
    public async Task RateLimiting_UsesForwardedForHeader()
    {
        _controller.ControllerContext.HttpContext!.Request.Headers["X-Forwarded-For"] = "203.0.113.5, 10.0.0.2";

        _pricingServiceMock.Setup(s => s.GetActivePricingTiersAsync())
            .ReturnsAsync(new List<PricingTierDto>());

        for (int i = 0; i < 100; i++)
        {
            await _controller.GetTiers();
        }

        var result = await _controller.GetTiers();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(429, statusResult.StatusCode);
    }

    #endregion
}
