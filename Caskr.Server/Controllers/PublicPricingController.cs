using Caskr.server.Services.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Caskr.server.Controllers;

/// <summary>
/// Public API for pricing information. No authentication required.
/// Endpoints are rate-limited and cached for performance.
/// </summary>
[ApiController]
[Route("api/public/pricing")]
[AllowAnonymous]
[Produces("application/json")]
public class PublicPricingController : ControllerBase
{
    private readonly IPricingService _pricingService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublicPricingController> _logger;

    private const int MaxRequestsPerMinute = 100;
    private const string RateLimitCacheKeyPrefix = "pricing:ratelimit:";

    public PublicPricingController(
        IPricingService pricingService,
        IMemoryCache cache,
        ILogger<PublicPricingController> logger)
    {
        _pricingService = pricingService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets the complete pricing page data including tiers, features, and FAQs.
    /// </summary>
    /// <remarks>
    /// This endpoint returns all data needed to render the public pricing page.
    /// Response is cached for 5 minutes.
    /// </remarks>
    /// <response code="200">Returns the pricing page data</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpGet]
    [ProducesResponseType(typeof(PricingPageDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<PricingPageDataDto>> GetPricingPageData()
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        var data = await _pricingService.GetPricingPageDataAsync();
        return Ok(data);
    }

    /// <summary>
    /// Gets all active pricing tiers with their features.
    /// </summary>
    /// <remarks>
    /// Returns tiers sorted by sort_order. Each tier includes its features
    /// with limit values and inclusion status.
    /// </remarks>
    /// <response code="200">Returns the list of pricing tiers</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpGet("tiers")]
    [ProducesResponseType(typeof(IEnumerable<PricingTierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IEnumerable<PricingTierDto>>> GetTiers()
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        var tiers = await _pricingService.GetActivePricingTiersAsync();
        return Ok(tiers);
    }

    /// <summary>
    /// Gets a single pricing tier by its slug.
    /// </summary>
    /// <param name="slug">The URL-friendly tier identifier (e.g., "craft", "growth", "professional", "enterprise")</param>
    /// <remarks>
    /// Returns detailed tier information including all features.
    /// </remarks>
    /// <response code="200">Returns the pricing tier</response>
    /// <response code="404">Tier not found</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpGet("tiers/{slug}")]
    [ProducesResponseType(typeof(PricingTierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<PricingTierDto>> GetTierBySlug(string slug)
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        var tier = await _pricingService.GetTierBySlugAsync(slug);
        if (tier == null)
        {
            return NotFound(new { message = $"Tier '{slug}' not found" });
        }

        return Ok(tier);
    }

    /// <summary>
    /// Gets all pricing features grouped by category.
    /// </summary>
    /// <remarks>
    /// Returns features organized by category (e.g., Compliance, Inventory, Reporting, Support).
    /// </remarks>
    /// <response code="200">Returns the list of feature categories</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpGet("features")]
    [ProducesResponseType(typeof(IEnumerable<PricingFeatureCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IEnumerable<PricingFeatureCategoryDto>>> GetFeatures()
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        var features = await _pricingService.GetPricingFeaturesAsync();
        return Ok(features);
    }

    /// <summary>
    /// Gets all active pricing FAQs.
    /// </summary>
    /// <remarks>
    /// Returns frequently asked questions sorted by display order.
    /// Answers may contain markdown formatting.
    /// </remarks>
    /// <response code="200">Returns the list of FAQs</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpGet("faqs")]
    [ProducesResponseType(typeof(IEnumerable<PricingFaqDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IEnumerable<PricingFaqDto>>> GetFaqs()
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        var faqs = await _pricingService.GetPricingFaqsAsync();
        return Ok(faqs);
    }

    /// <summary>
    /// Validates a promotional code.
    /// </summary>
    /// <param name="request">The promo code validation request</param>
    /// <remarks>
    /// Checks if the promo code is valid, not expired, and hasn't exceeded max redemptions.
    /// Optionally validates if the code applies to a specific tier.
    /// </remarks>
    /// <response code="200">Returns the validation result</response>
    /// <response code="400">Invalid request</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("validate-promo")]
    [ProducesResponseType(typeof(PromoCodeValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PromoCodeValidationResult>> ValidatePromoCode([FromBody] ValidatePromoCodeRequest request)
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Promo code is required" });
        }

        var result = await _pricingService.ValidatePromoCodeAsync(request.Code, request.TierId);
        return Ok(result);
    }

    /// <summary>
    /// Applies a promotional code to a tier and returns the discounted price.
    /// </summary>
    /// <param name="request">The promo code application request</param>
    /// <remarks>
    /// Calculates the discounted price based on the promo code type
    /// (percentage, fixed amount, or free months).
    /// </remarks>
    /// <response code="200">Returns the application result with discounted prices</response>
    /// <response code="400">Invalid request or promo code not applicable</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("apply-promo")]
    [ProducesResponseType(typeof(PromoCodeApplicationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PromoCodeApplicationResult>> ApplyPromoCode([FromBody] ApplyPromoCodeRequest request)
    {
        if (IsRateLimited())
        {
            return StatusCode(429, new { message = "Rate limit exceeded. Please try again later." });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Promo code is required" });
        }

        if (request.TierId <= 0)
        {
            return BadRequest(new { message = "Valid tier ID is required" });
        }

        var result = await _pricingService.ApplyPromoCodeAsync(request.Code, request.TierId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Checks if the current request should be rate limited.
    /// Uses a sliding window of 1 minute with max 100 requests per IP.
    /// </summary>
    private bool IsRateLimited()
    {
        var clientIp = GetClientIpAddress();
        if (string.IsNullOrEmpty(clientIp))
        {
            return false; // Allow if we can't determine IP
        }

        var cacheKey = $"{RateLimitCacheKeyPrefix}{clientIp}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        if (_cache.TryGetValue(cacheKey, out List<DateTime>? timestamps) && timestamps != null)
        {
            // Remove timestamps outside the window
            timestamps = timestamps.Where(t => t > windowStart).ToList();

            if (timestamps.Count >= MaxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", clientIp);
                return true;
            }

            timestamps.Add(now);
        }
        else
        {
            timestamps = [now];
        }

        _cache.Set(cacheKey, timestamps, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });

        return false;
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    private string? GetClientIpAddress()
    {
        // Check X-Forwarded-For header for proxied requests
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
