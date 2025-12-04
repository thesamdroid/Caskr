using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Models.Pricing;
using Caskr.server.Services;
using Caskr.server.Services.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Controllers.Admin;

/// <summary>
/// Admin API for managing pricing data. Requires admin role authentication.
/// All changes are logged to the audit trail.
/// </summary>
[ApiController]
[Route("api/admin/pricing")]
[Produces("application/json")]
public class PricingAdminController : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _context;
    private readonly IPricingAuditLogger _auditLogger;
    private readonly IPricingService _pricingService;
    private readonly IUsersService _usersService;
    private readonly ILogger<PricingAdminController> _logger;

    public PricingAdminController(
        CaskrDbContext context,
        IPricingAuditLogger auditLogger,
        IPricingService pricingService,
        IUsersService usersService,
        ILogger<PricingAdminController> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _pricingService = pricingService;
        _usersService = usersService;
        _logger = logger;
    }

    #region Pricing Tiers

    /// <summary>
    /// Gets all pricing tiers (including inactive).
    /// </summary>
    [HttpGet("tiers")]
    [ProducesResponseType(typeof(IEnumerable<PricingTier>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PricingTier>>> GetAllTiers()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var tiers = await _context.PricingTiers
            .OrderBy(t => t.SortOrder)
            .Include(t => t.TierFeatures)
                .ThenInclude(tf => tf.Feature)
            .AsNoTracking()
            .ToListAsync();

        return Ok(tiers);
    }

    /// <summary>
    /// Gets a single pricing tier by ID.
    /// </summary>
    [HttpGet("tiers/{id}")]
    [ProducesResponseType(typeof(PricingTier), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingTier>> GetTier(int id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var tier = await _context.PricingTiers
            .Include(t => t.TierFeatures)
                .ThenInclude(tf => tf.Feature)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tier == null)
        {
            return NotFound();
        }

        return Ok(tier);
    }

    /// <summary>
    /// Creates a new pricing tier.
    /// </summary>
    [HttpPost("tiers")]
    [ProducesResponseType(typeof(PricingTier), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingTier>> CreateTier([FromBody] PricingTier tier)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(tier.Name) || string.IsNullOrWhiteSpace(tier.Slug))
        {
            return BadRequest(new { message = "Name and slug are required" });
        }

        // Check for duplicate slug
        if (await _context.PricingTiers.AnyAsync(t => t.Slug == tier.Slug))
        {
            return BadRequest(new { message = "A tier with this slug already exists" });
        }

        tier.CreatedAt = DateTime.UtcNow;
        tier.UpdatedAt = DateTime.UtcNow;

        _context.PricingTiers.Add(tier);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Create, tier, null, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Created pricing tier '{Name}' (ID: {Id})", tier.Name, tier.Id);

        return CreatedAtAction(nameof(GetTier), new { id = tier.Id }, tier);
    }

    /// <summary>
    /// Updates an existing pricing tier.
    /// </summary>
    [HttpPut("tiers/{id}")]
    [ProducesResponseType(typeof(PricingTier), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingTier>> UpdateTier(int id, [FromBody] PricingTier tier)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (id != tier.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var existingTier = await _context.PricingTiers.FindAsync(id);
        if (existingTier == null)
        {
            return NotFound();
        }

        // Check for duplicate slug (excluding current tier)
        if (await _context.PricingTiers.AnyAsync(t => t.Slug == tier.Slug && t.Id != id))
        {
            return BadRequest(new { message = "A tier with this slug already exists" });
        }

        var oldTier = CloneTier(existingTier);

        existingTier.Name = tier.Name;
        existingTier.Slug = tier.Slug;
        existingTier.Tagline = tier.Tagline;
        existingTier.MonthlyPriceCents = tier.MonthlyPriceCents;
        existingTier.AnnualPriceCents = tier.AnnualPriceCents;
        existingTier.AnnualDiscountPercent = tier.AnnualDiscountPercent;
        existingTier.IsPopular = tier.IsPopular;
        existingTier.IsCustomPricing = tier.IsCustomPricing;
        existingTier.CtaText = tier.CtaText;
        existingTier.CtaUrl = tier.CtaUrl;
        existingTier.SortOrder = tier.SortOrder;
        existingTier.IsActive = tier.IsActive;
        existingTier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Update, existingTier, oldTier, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Updated pricing tier '{Name}' (ID: {Id})", tier.Name, tier.Id);

        return Ok(existingTier);
    }

    /// <summary>
    /// Deletes a pricing tier.
    /// </summary>
    [HttpDelete("tiers/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTier(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var tier = await _context.PricingTiers.FindAsync(id);
        if (tier == null)
        {
            return NotFound();
        }

        _context.PricingTiers.Remove(tier);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Delete, null, tier, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Deleted pricing tier '{Name}' (ID: {Id})", tier.Name, tier.Id);

        return NoContent();
    }

    #endregion

    #region Pricing Features

    /// <summary>
    /// Gets all pricing features.
    /// </summary>
    [HttpGet("features")]
    [ProducesResponseType(typeof(IEnumerable<PricingFeature>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PricingFeature>>> GetAllFeatures()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var features = await _context.PricingFeatures
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return Ok(features);
    }

    /// <summary>
    /// Creates a new pricing feature.
    /// </summary>
    [HttpPost("features")]
    [ProducesResponseType(typeof(PricingFeature), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingFeature>> CreateFeature([FromBody] PricingFeature feature)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(feature.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        feature.CreatedAt = DateTime.UtcNow;
        feature.UpdatedAt = DateTime.UtcNow;

        _context.PricingFeatures.Add(feature);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Create, feature, null, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Created pricing feature '{Name}' (ID: {Id})", feature.Name, feature.Id);

        return CreatedAtAction(nameof(GetAllFeatures), new { id = feature.Id }, feature);
    }

    /// <summary>
    /// Updates an existing pricing feature.
    /// </summary>
    [HttpPut("features/{id}")]
    [ProducesResponseType(typeof(PricingFeature), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingFeature>> UpdateFeature(int id, [FromBody] PricingFeature feature)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (id != feature.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var existingFeature = await _context.PricingFeatures.FindAsync(id);
        if (existingFeature == null)
        {
            return NotFound();
        }

        var oldFeature = CloneFeature(existingFeature);

        existingFeature.Name = feature.Name;
        existingFeature.Description = feature.Description;
        existingFeature.Category = feature.Category;
        existingFeature.SortOrder = feature.SortOrder;
        existingFeature.IsActive = feature.IsActive;
        existingFeature.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Update, existingFeature, oldFeature, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Updated pricing feature '{Name}' (ID: {Id})", feature.Name, feature.Id);

        return Ok(existingFeature);
    }

    /// <summary>
    /// Deletes a pricing feature.
    /// </summary>
    [HttpDelete("features/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteFeature(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var feature = await _context.PricingFeatures.FindAsync(id);
        if (feature == null)
        {
            return NotFound();
        }

        _context.PricingFeatures.Remove(feature);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Delete, null, feature, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Deleted pricing feature '{Name}' (ID: {Id})", feature.Name, feature.Id);

        return NoContent();
    }

    #endregion

    #region Tier Features (Junction)

    /// <summary>
    /// Associates a feature with a tier.
    /// </summary>
    [HttpPost("tiers/{tierId}/features")]
    [ProducesResponseType(typeof(PricingTierFeature), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingTierFeature>> AddFeatureToTier(int tierId, [FromBody] PricingTierFeature tierFeature)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        tierFeature.TierId = tierId;

        // Validate tier exists
        if (!await _context.PricingTiers.AnyAsync(t => t.Id == tierId))
        {
            return NotFound(new { message = "Tier not found" });
        }

        // Validate feature exists
        if (!await _context.PricingFeatures.AnyAsync(f => f.Id == tierFeature.FeatureId))
        {
            return NotFound(new { message = "Feature not found" });
        }

        // Check for duplicate
        if (await _context.PricingTierFeatures.AnyAsync(tf => tf.TierId == tierId && tf.FeatureId == tierFeature.FeatureId))
        {
            return BadRequest(new { message = "This feature is already associated with this tier" });
        }

        _context.PricingTierFeatures.Add(tierFeature);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Create, tierFeature, null, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Added feature {FeatureId} to tier {TierId}", tierFeature.FeatureId, tierId);

        return CreatedAtAction(nameof(GetTier), new { id = tierId }, tierFeature);
    }

    /// <summary>
    /// Updates a tier-feature association.
    /// </summary>
    [HttpPut("tiers/{tierId}/features/{featureId}")]
    [ProducesResponseType(typeof(PricingTierFeature), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingTierFeature>> UpdateTierFeature(int tierId, int featureId, [FromBody] PricingTierFeature tierFeature)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var existingTierFeature = await _context.PricingTierFeatures
            .FirstOrDefaultAsync(tf => tf.TierId == tierId && tf.FeatureId == featureId);

        if (existingTierFeature == null)
        {
            return NotFound();
        }

        var oldTierFeature = CloneTierFeature(existingTierFeature);

        existingTierFeature.IsIncluded = tierFeature.IsIncluded;
        existingTierFeature.LimitValue = tierFeature.LimitValue;
        existingTierFeature.LimitDescription = tierFeature.LimitDescription;

        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Update, existingTierFeature, oldTierFeature, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Updated feature {FeatureId} for tier {TierId}", featureId, tierId);

        return Ok(existingTierFeature);
    }

    /// <summary>
    /// Removes a feature from a tier.
    /// </summary>
    [HttpDelete("tiers/{tierId}/features/{featureId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveFeatureFromTier(int tierId, int featureId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var tierFeature = await _context.PricingTierFeatures
            .FirstOrDefaultAsync(tf => tf.TierId == tierId && tf.FeatureId == featureId);

        if (tierFeature == null)
        {
            return NotFound();
        }

        _context.PricingTierFeatures.Remove(tierFeature);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Delete, null, tierFeature, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Removed feature {FeatureId} from tier {TierId}", featureId, tierId);

        return NoContent();
    }

    #endregion

    #region FAQs

    /// <summary>
    /// Gets all pricing FAQs.
    /// </summary>
    [HttpGet("faqs")]
    [ProducesResponseType(typeof(IEnumerable<PricingFaq>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PricingFaq>>> GetAllFaqs()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var faqs = await _context.PricingFaqs
            .OrderBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return Ok(faqs);
    }

    /// <summary>
    /// Creates a new FAQ.
    /// </summary>
    [HttpPost("faqs")]
    [ProducesResponseType(typeof(PricingFaq), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingFaq>> CreateFaq([FromBody] PricingFaq faq)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(faq.Question) || string.IsNullOrWhiteSpace(faq.Answer))
        {
            return BadRequest(new { message = "Question and answer are required" });
        }

        faq.CreatedAt = DateTime.UtcNow;
        faq.UpdatedAt = DateTime.UtcNow;

        _context.PricingFaqs.Add(faq);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Create, faq, null, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Created pricing FAQ (ID: {Id})", faq.Id);

        return CreatedAtAction(nameof(GetAllFaqs), new { id = faq.Id }, faq);
    }

    /// <summary>
    /// Updates an existing FAQ.
    /// </summary>
    [HttpPut("faqs/{id}")]
    [ProducesResponseType(typeof(PricingFaq), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingFaq>> UpdateFaq(int id, [FromBody] PricingFaq faq)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (id != faq.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var existingFaq = await _context.PricingFaqs.FindAsync(id);
        if (existingFaq == null)
        {
            return NotFound();
        }

        var oldFaq = CloneFaq(existingFaq);

        existingFaq.Question = faq.Question;
        existingFaq.Answer = faq.Answer;
        existingFaq.SortOrder = faq.SortOrder;
        existingFaq.IsActive = faq.IsActive;
        existingFaq.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Update, existingFaq, oldFaq, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Updated pricing FAQ (ID: {Id})", faq.Id);

        return Ok(existingFaq);
    }

    /// <summary>
    /// Deletes a FAQ.
    /// </summary>
    [HttpDelete("faqs/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteFaq(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var faq = await _context.PricingFaqs.FindAsync(id);
        if (faq == null)
        {
            return NotFound();
        }

        _context.PricingFaqs.Remove(faq);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Delete, null, faq, userId);
        _pricingService.InvalidateCache();

        _logger.LogInformation("Deleted pricing FAQ (ID: {Id})", faq.Id);

        return NoContent();
    }

    #endregion

    #region Promotions

    /// <summary>
    /// Gets all pricing promotions.
    /// </summary>
    [HttpGet("promotions")]
    [ProducesResponseType(typeof(IEnumerable<PricingPromotion>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PricingPromotion>>> GetAllPromotions()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var promotions = await _context.PricingPromotions
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return Ok(promotions);
    }

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    [HttpPost("promotions")]
    [ProducesResponseType(typeof(PricingPromotion), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingPromotion>> CreatePromotion([FromBody] PricingPromotion promotion)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(promotion.Code))
        {
            return BadRequest(new { message = "Code is required" });
        }

        // Check for duplicate code
        if (await _context.PricingPromotions.AnyAsync(p => p.Code.ToLower() == promotion.Code.ToLower()))
        {
            return BadRequest(new { message = "A promotion with this code already exists" });
        }

        promotion.CreatedAt = DateTime.UtcNow;
        promotion.UpdatedAt = DateTime.UtcNow;
        promotion.CurrentRedemptions = 0;

        _context.PricingPromotions.Add(promotion);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Create, promotion, null, userId);

        _logger.LogInformation("Created pricing promotion '{Code}' (ID: {Id})", promotion.Code, promotion.Id);

        return CreatedAtAction(nameof(GetAllPromotions), new { id = promotion.Id }, promotion);
    }

    /// <summary>
    /// Updates an existing promotion.
    /// </summary>
    [HttpPut("promotions/{id}")]
    [ProducesResponseType(typeof(PricingPromotion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingPromotion>> UpdatePromotion(int id, [FromBody] PricingPromotion promotion)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (id != promotion.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var existingPromotion = await _context.PricingPromotions.FindAsync(id);
        if (existingPromotion == null)
        {
            return NotFound();
        }

        // Check for duplicate code (excluding current promotion)
        if (await _context.PricingPromotions.AnyAsync(p => p.Code.ToLower() == promotion.Code.ToLower() && p.Id != id))
        {
            return BadRequest(new { message = "A promotion with this code already exists" });
        }

        var oldPromotion = ClonePromotion(existingPromotion);

        existingPromotion.Code = promotion.Code;
        existingPromotion.Description = promotion.Description;
        existingPromotion.DiscountType = promotion.DiscountType;
        existingPromotion.DiscountValue = promotion.DiscountValue;
        existingPromotion.AppliesToTiersJson = promotion.AppliesToTiersJson;
        existingPromotion.ValidFrom = promotion.ValidFrom;
        existingPromotion.ValidUntil = promotion.ValidUntil;
        existingPromotion.MaxRedemptions = promotion.MaxRedemptions;
        existingPromotion.IsActive = promotion.IsActive;
        existingPromotion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Update, existingPromotion, oldPromotion, userId);

        _logger.LogInformation("Updated pricing promotion '{Code}' (ID: {Id})", promotion.Code, promotion.Id);

        return Ok(existingPromotion);
    }

    /// <summary>
    /// Deletes a promotion.
    /// </summary>
    [HttpDelete("promotions/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePromotion(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var promotion = await _context.PricingPromotions.FindAsync(id);
        if (promotion == null)
        {
            return NotFound();
        }

        _context.PricingPromotions.Remove(promotion);
        await _context.SaveChangesAsync();

        await _auditLogger.LogChangeAsync(PricingAuditAction.Delete, null, promotion, userId);

        _logger.LogInformation("Deleted pricing promotion '{Code}' (ID: {Id})", promotion.Code, promotion.Id);

        return NoContent();
    }

    #endregion

    #region Audit Logs

    /// <summary>
    /// Gets pricing audit logs.
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(IEnumerable<PricingAuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PricingAuditLog>>> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var logs = await _auditLogger.GetAuditLogsAsync(entityType, entityId, startDate, endDate, limit);
        return Ok(logs);
    }

    #endregion

    #region Preview

    /// <summary>
    /// Gets a preview of how pricing would look with pending changes.
    /// </summary>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(PricingPageDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingPageDataDto>> GetPreview()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        // For preview, we bypass the cache and get fresh data including inactive items
        var tiers = await _context.PricingTiers
            .OrderBy(t => t.SortOrder)
            .Include(t => t.TierFeatures)
                .ThenInclude(tf => tf.Feature)
            .AsNoTracking()
            .ToListAsync();

        var features = await _context.PricingFeatures
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var faqs = await _context.PricingFaqs
            .OrderBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var result = new PricingPageDataDto
        {
            Tiers = tiers.Select(t => new PricingTierDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Tagline = t.Tagline,
                MonthlyPriceCents = t.MonthlyPriceCents,
                AnnualPriceCents = t.AnnualPriceCents,
                AnnualDiscountPercent = t.AnnualDiscountPercent,
                IsPopular = t.IsPopular,
                IsCustomPricing = t.IsCustomPricing,
                CtaText = t.CtaText,
                CtaUrl = t.CtaUrl,
                SortOrder = t.SortOrder,
                Features = t.TierFeatures
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
            }).ToList(),
            FeaturesByCategory = features
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
                .ToList(),
            Faqs = faqs.Select(f => new PricingFaqDto
            {
                Id = f.Id,
                Question = f.Question,
                Answer = f.Answer,
                SortOrder = f.SortOrder
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(result);
    }

    #endregion

    #region Helper Methods

    private async Task<bool> IsAdminAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return false;
        }

        return (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
    }

    private async Task<int> GetCurrentUserIdAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return 0;
        }
        return userId;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == 0)
        {
            return null;
        }
        return await _usersService.GetUserByIdAsync(userId);
    }

    private static PricingTier CloneTier(PricingTier tier) => new()
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
        IsActive = tier.IsActive,
        CreatedAt = tier.CreatedAt,
        UpdatedAt = tier.UpdatedAt
    };

    private static PricingFeature CloneFeature(PricingFeature feature) => new()
    {
        Id = feature.Id,
        Name = feature.Name,
        Description = feature.Description,
        Category = feature.Category,
        SortOrder = feature.SortOrder,
        IsActive = feature.IsActive,
        CreatedAt = feature.CreatedAt,
        UpdatedAt = feature.UpdatedAt
    };

    private static PricingTierFeature CloneTierFeature(PricingTierFeature tf) => new()
    {
        Id = tf.Id,
        TierId = tf.TierId,
        FeatureId = tf.FeatureId,
        IsIncluded = tf.IsIncluded,
        LimitValue = tf.LimitValue,
        LimitDescription = tf.LimitDescription
    };

    private static PricingFaq CloneFaq(PricingFaq faq) => new()
    {
        Id = faq.Id,
        Question = faq.Question,
        Answer = faq.Answer,
        SortOrder = faq.SortOrder,
        IsActive = faq.IsActive,
        CreatedAt = faq.CreatedAt,
        UpdatedAt = faq.UpdatedAt
    };

    private static PricingPromotion ClonePromotion(PricingPromotion promo) => new()
    {
        Id = promo.Id,
        Code = promo.Code,
        Description = promo.Description,
        DiscountType = promo.DiscountType,
        DiscountValue = promo.DiscountValue,
        AppliesToTiersJson = promo.AppliesToTiersJson,
        ValidFrom = promo.ValidFrom,
        ValidUntil = promo.ValidUntil,
        MaxRedemptions = promo.MaxRedemptions,
        CurrentRedemptions = promo.CurrentRedemptions,
        IsActive = promo.IsActive,
        CreatedAt = promo.CreatedAt,
        UpdatedAt = promo.UpdatedAt
    };

    #endregion
}
