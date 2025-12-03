using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

/// <summary>
/// API response for device detection
/// </summary>
public class DeviceDetectionResponse
{
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public bool IsMobile { get; set; }
    public bool IsTablet { get; set; }
    public bool IsBot { get; set; }
    public bool HasTouchCapability { get; set; }
    public string RecommendedSite { get; set; } = "desktop";
    public SitePreferenceResponse? UserPreference { get; set; }
}

/// <summary>
/// Request to save site preference
/// </summary>
public class SaveSitePreferenceRequest
{
    public string PreferredSite { get; set; } = "auto";
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
}

[ApiController]
[Route("api/mobile")]
public class MobileDetectionController : ControllerBase
{
    private readonly IMobileDetectionService _mobileDetectionService;
    private readonly ILogger<MobileDetectionController> _logger;

    private const string SessionIdCookie = "caskr_session_id";

    public MobileDetectionController(
        IMobileDetectionService mobileDetectionService,
        ILogger<MobileDetectionController> logger)
    {
        _mobileDetectionService = mobileDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Detect device type from User-Agent and return recommended site
    /// </summary>
    [HttpGet("detect")]
    [AllowAnonymous]
    public async Task<ActionResult<DeviceDetectionResponse>> DetectDevice([FromQuery] int? screenWidth)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var detection = _mobileDetectionService.DetectDevice(userAgent);

            var response = new DeviceDetectionResponse
            {
                DeviceType = detection.DeviceType.ToString(),
                DeviceName = detection.DeviceName,
                Browser = detection.Browser,
                OperatingSystem = detection.OperatingSystem,
                IsMobile = detection.IsMobile,
                IsTablet = detection.IsTablet,
                IsBot = detection.IsBot,
                HasTouchCapability = detection.HasTouchCapability,
                RecommendedSite = _mobileDetectionService.GetRecommendedSite(detection, screenWidth)
            };

            // Try to get user preference
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var preference = await _mobileDetectionService.GetPreferenceByUserIdAsync(userId.Value);
                if (preference != null)
                {
                    response.UserPreference = new SitePreferenceResponse
                    {
                        PreferredSite = preference.PreferredSite,
                        LastDetectedDevice = preference.LastDetectedDevice,
                        UpdatedAt = preference.UpdatedAt
                    };

                    // Override recommended site if user has explicit preference
                    if (preference.PreferredSite != SitePreference.Auto)
                    {
                        response.RecommendedSite = preference.PreferredSite == SitePreference.Mobile ? "mobile" : "desktop";
                    }
                }
            }
            else
            {
                // Check for anonymous session
                var sessionId = GetOrCreateSessionId();
                var preference = await _mobileDetectionService.GetPreferenceBySessionIdAsync(sessionId);
                if (preference != null)
                {
                    response.UserPreference = new SitePreferenceResponse
                    {
                        PreferredSite = preference.PreferredSite,
                        LastDetectedDevice = preference.LastDetectedDevice,
                        UpdatedAt = preference.UpdatedAt
                    };

                    // Override recommended site if user has explicit preference
                    if (preference.PreferredSite != SitePreference.Auto)
                    {
                        response.RecommendedSite = preference.PreferredSite == SitePreference.Mobile ? "mobile" : "desktop";
                    }
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting device");
            return StatusCode(500, new { message = "Error detecting device" });
        }
    }

    /// <summary>
    /// Save user site preference
    /// </summary>
    [HttpPost("preference")]
    [AllowAnonymous]
    public async Task<ActionResult<SitePreferenceResponse>> SavePreference([FromBody] SaveSitePreferenceRequest request)
    {
        try
        {
            if (!TryParseSitePreference(request.PreferredSite, out var preference))
            {
                return BadRequest(new { message = "Invalid preference value. Must be 'auto', 'desktop', or 'mobile'" });
            }

            var saveRequest = new SavePreferenceRequest
            {
                PreferredSite = preference,
                ScreenWidth = request.ScreenWidth,
                ScreenHeight = request.ScreenHeight
            };

            var userAgent = Request.Headers.UserAgent.ToString();
            UserSitePreference saved;

            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                saved = await _mobileDetectionService.SavePreferenceAsync(userId.Value, saveRequest, userAgent);
            }
            else
            {
                var sessionId = GetOrCreateSessionId();
                saved = await _mobileDetectionService.SaveAnonymousPreferenceAsync(sessionId, saveRequest, userAgent);
            }

            return Ok(new SitePreferenceResponse
            {
                PreferredSite = saved.PreferredSite,
                LastDetectedDevice = saved.LastDetectedDevice,
                UpdatedAt = saved.UpdatedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid preference request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving preference");
            return StatusCode(500, new { message = "Error saving preference" });
        }
    }

    /// <summary>
    /// Get current user site preference
    /// </summary>
    [HttpGet("preference")]
    [AllowAnonymous]
    public async Task<ActionResult<SitePreferenceResponse>> GetPreference()
    {
        try
        {
            UserSitePreference? preference = null;

            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                preference = await _mobileDetectionService.GetPreferenceByUserIdAsync(userId.Value);
            }
            else
            {
                var sessionId = GetSessionId();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    preference = await _mobileDetectionService.GetPreferenceBySessionIdAsync(sessionId);
                }
            }

            if (preference == null)
            {
                return Ok(new SitePreferenceResponse
                {
                    PreferredSite = SitePreference.Auto,
                    LastDetectedDevice = null,
                    UpdatedAt = null
                });
            }

            return Ok(new SitePreferenceResponse
            {
                PreferredSite = preference.PreferredSite,
                LastDetectedDevice = preference.LastDetectedDevice,
                UpdatedAt = preference.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preference");
            return StatusCode(500, new { message = "Error getting preference" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private string GetOrCreateSessionId()
    {
        var sessionId = GetSessionId();
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(SessionIdCookie, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }
        return sessionId;
    }

    private string? GetSessionId()
    {
        return Request.Cookies[SessionIdCookie];
    }

    private static bool TryParseSitePreference(string? value, out SitePreference preference)
    {
        preference = SitePreference.Auto;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // Default to Auto
        }

        return value.ToLowerInvariant() switch
        {
            "auto" => true,
            "desktop" => SetAndReturn(out preference, SitePreference.Desktop),
            "mobile" => SetAndReturn(out preference, SitePreference.Mobile),
            _ => false
        };
    }

    private static bool SetAndReturn(out SitePreference preference, SitePreference value)
    {
        preference = value;
        return true;
    }
}
