using System.Text.RegularExpressions;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Caskr.server.Services;

/// <summary>
/// Device type detection result
/// </summary>
public enum DeviceType
{
    Desktop = 0,
    Mobile = 1,
    Tablet = 2,
    Bot = 3,
    Unknown = 4
}

/// <summary>
/// Result of mobile device detection
/// </summary>
public class DeviceDetectionResult
{
    public DeviceType DeviceType { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public bool IsMobile { get; set; }
    public bool IsTablet { get; set; }
    public bool IsBot { get; set; }
    public bool HasTouchCapability { get; set; }
    public string RecommendedSite { get; set; } = "desktop";
    public string RawUserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Request to save user site preference
/// </summary>
public class SavePreferenceRequest
{
    public SitePreference PreferredSite { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
}

/// <summary>
/// Response containing user site preference
/// </summary>
public class SitePreferenceResponse
{
    public SitePreference PreferredSite { get; set; }
    public string? LastDetectedDevice { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public interface IMobileDetectionService
{
    /// <summary>
    /// Detects device type from User-Agent string
    /// </summary>
    DeviceDetectionResult DetectDevice(string? userAgent);

    /// <summary>
    /// Determines if a device should be redirected to mobile site
    /// </summary>
    bool ShouldRedirectToMobile(string? userAgent, SitePreference? userPreference = null);

    /// <summary>
    /// Determines if a device should be redirected to desktop site
    /// </summary>
    bool ShouldRedirectToDesktop(string? userAgent, SitePreference? userPreference = null);

    /// <summary>
    /// Gets user site preference by user ID
    /// </summary>
    Task<UserSitePreference?> GetPreferenceByUserIdAsync(int userId);

    /// <summary>
    /// Gets user site preference by session ID
    /// </summary>
    Task<UserSitePreference?> GetPreferenceBySessionIdAsync(string sessionId);

    /// <summary>
    /// Saves user site preference for authenticated user
    /// </summary>
    Task<UserSitePreference> SavePreferenceAsync(int userId, SavePreferenceRequest request, string? userAgent);

    /// <summary>
    /// Saves user site preference for anonymous user
    /// </summary>
    Task<UserSitePreference> SaveAnonymousPreferenceAsync(string sessionId, SavePreferenceRequest request, string? userAgent);

    /// <summary>
    /// Determines recommended site based on device detection and screen size
    /// </summary>
    string GetRecommendedSite(DeviceDetectionResult detection, int? screenWidth = null);
}

public class MobileDetectionService : IMobileDetectionService
{
    private readonly CaskrDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MobileDetectionService> _logger;

    // Cache duration for detection results
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    // Mobile breakpoint width in pixels
    private const int MobileBreakpoint = 768;

    // User-Agent patterns for mobile detection
    private static readonly Regex MobileRegex = new(
        @"Mobile|iP(hone|od)|Android.*Mobile|Windows Phone|BlackBerry|BB10|Opera Mini|IEMobile|Opera Mobi|webOS|Fennec|Minimo|NetFront|Polaris|SEMC-Browser|Skyfire|Symphony|UP\.Browser|webOS|Palm|Symbian|Maemo|MIDP|Windows CE|Obigo|Dolfin|DoCoMo|KDDI|Vodafone|HTC|LG|MOT|Nokia|Samsung|SonyEricsson|ZTE",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Tablet detection patterns
    private static readonly Regex TabletRegex = new(
        @"iPad|Android(?!.*Mobile)|Tablet|Kindle|Silk|PlayBook|Xoom|GT-P|SCH-I|SM-T|SAMSUNG.*Tablet|Nexus (7|9|10)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Desktop browser patterns
    private static readonly Regex DesktopBrowserRegex = new(
        @"Windows NT|Macintosh|X11|Linux(?!.*Android)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Bot/Crawler patterns
    private static readonly Regex BotRegex = new(
        @"bot|crawler|spider|scraper|Googlebot|Bingbot|Slurp|DuckDuckBot|Baiduspider|YandexBot|Sogou|Exabot|facebot|facebookexternalhit|ia_archiver|linkedinbot|twitterbot|pinterest|slack|telegram|whatsapp|discord|curl|wget|python-requests|Go-http-client|Java|Apache-HttpClient",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // iPhone detection
    private static readonly Regex IPhoneRegex = new(
        @"iPhone",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // iPad detection
    private static readonly Regex IPadRegex = new(
        @"iPad",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Android phone detection (Android without Tablet indicators)
    private static readonly Regex AndroidPhoneRegex = new(
        @"Android.*Mobile",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Android tablet detection
    private static readonly Regex AndroidTabletRegex = new(
        @"Android(?!.*Mobile)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Windows Phone detection
    private static readonly Regex WindowsPhoneRegex = new(
        @"Windows Phone|IEMobile",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Browser detection patterns
    private static readonly Regex ChromeRegex = new(@"Chrome/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SafariRegex = new(@"Safari/[\d\.]+(?!.*Chrome)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FirefoxRegex = new(@"Firefox/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EdgeRegex = new(@"Edg/[\d\.]+|Edge/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OperaRegex = new(@"OPR/[\d\.]+|Opera/[\d\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // OS detection patterns
    private static readonly Regex IOSRegex = new(@"iPhone OS ([\d_]+)|iPad.*OS ([\d_]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AndroidOSRegex = new(@"Android ([\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WindowsRegex = new(@"Windows NT ([\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MacOSRegex = new(@"Mac OS X ([\d_\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinuxRegex = new(@"Linux", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MobileDetectionService(
        CaskrDbContext dbContext,
        IMemoryCache cache,
        ILogger<MobileDetectionService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public DeviceDetectionResult DetectDevice(string? userAgent)
    {
        var result = new DeviceDetectionResult
        {
            RawUserAgent = userAgent ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            result.DeviceType = DeviceType.Unknown;
            result.RecommendedSite = "desktop";
            return result;
        }

        // Truncate very long User-Agent strings for safety
        if (userAgent.Length > 2000)
        {
            userAgent = userAgent.Substring(0, 2000);
            _logger.LogWarning("User-Agent string truncated from {OriginalLength} characters", result.RawUserAgent.Length);
        }

        // Check cache first
        var cacheKey = $"device_detection_{ComputeHash(userAgent)}";
        if (_cache.TryGetValue<DeviceDetectionResult>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        // Detect bot first (bots should not be redirected)
        if (BotRegex.IsMatch(userAgent))
        {
            result.DeviceType = DeviceType.Bot;
            result.IsBot = true;
            result.DeviceName = "Bot/Crawler";
            result.RecommendedSite = "desktop";
            CacheResult(cacheKey, result);
            return result;
        }

        // Detect device type and set properties
        result.Browser = DetectBrowser(userAgent);
        result.OperatingSystem = DetectOperatingSystem(userAgent);

        // Check for tablet first (tablets use mobile site)
        if (TabletRegex.IsMatch(userAgent))
        {
            result.DeviceType = DeviceType.Tablet;
            result.IsTablet = true;
            result.IsMobile = false; // Technically tablet, but goes to mobile site
            result.HasTouchCapability = true;
            result.DeviceName = DetectTabletName(userAgent);
            result.RecommendedSite = "mobile"; // Tablets use mobile site
            CacheResult(cacheKey, result);
            return result;
        }

        // Check for mobile
        if (MobileRegex.IsMatch(userAgent))
        {
            result.DeviceType = DeviceType.Mobile;
            result.IsMobile = true;
            result.HasTouchCapability = true;
            result.DeviceName = DetectMobileDeviceName(userAgent);
            result.RecommendedSite = "mobile";
            CacheResult(cacheKey, result);
            return result;
        }

        // Default to desktop
        if (DesktopBrowserRegex.IsMatch(userAgent))
        {
            result.DeviceType = DeviceType.Desktop;
            result.DeviceName = "Desktop";
            result.RecommendedSite = "desktop";
        }
        else
        {
            result.DeviceType = DeviceType.Unknown;
            result.DeviceName = "Unknown";
            result.RecommendedSite = "desktop";
        }

        CacheResult(cacheKey, result);
        return result;
    }

    public bool ShouldRedirectToMobile(string? userAgent, SitePreference? userPreference = null)
    {
        // If user explicitly chose desktop, don't redirect
        if (userPreference == SitePreference.Desktop)
        {
            return false;
        }

        // If user explicitly chose mobile, always redirect
        if (userPreference == SitePreference.Mobile)
        {
            return true;
        }

        // Auto mode: use detection
        var detection = DetectDevice(userAgent);

        // Don't redirect bots
        if (detection.IsBot)
        {
            return false;
        }

        return detection.IsMobile || detection.IsTablet;
    }

    public bool ShouldRedirectToDesktop(string? userAgent, SitePreference? userPreference = null)
    {
        // If user explicitly chose mobile, don't redirect
        if (userPreference == SitePreference.Mobile)
        {
            return false;
        }

        // If user explicitly chose desktop, always redirect
        if (userPreference == SitePreference.Desktop)
        {
            return true;
        }

        // Auto mode: use detection
        var detection = DetectDevice(userAgent);

        // Don't redirect bots
        if (detection.IsBot)
        {
            return false;
        }

        return detection.DeviceType == DeviceType.Desktop;
    }

    public async Task<UserSitePreference?> GetPreferenceByUserIdAsync(int userId)
    {
        return await _dbContext.Set<UserSitePreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<UserSitePreference?> GetPreferenceBySessionIdAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return await _dbContext.Set<UserSitePreference>()
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == null);
    }

    public async Task<UserSitePreference> SavePreferenceAsync(int userId, SavePreferenceRequest request, string? userAgent)
    {
        var detection = DetectDevice(userAgent);

        var preference = await GetPreferenceByUserIdAsync(userId);

        if (preference == null)
        {
            preference = new UserSitePreference
            {
                UserId = userId,
                PreferredSite = request.PreferredSite,
                LastDetectedDevice = detection.DeviceName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Set<UserSitePreference>().Add(preference);
        }
        else
        {
            preference.PreferredSite = request.PreferredSite;
            preference.LastDetectedDevice = detection.DeviceName;
            preference.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Saved site preference for user {UserId}: {Preference}", userId, request.PreferredSite);

        return preference;
    }

    public async Task<UserSitePreference> SaveAnonymousPreferenceAsync(string sessionId, SavePreferenceRequest request, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required for anonymous preferences", nameof(sessionId));
        }

        var detection = DetectDevice(userAgent);

        var preference = await GetPreferenceBySessionIdAsync(sessionId);

        if (preference == null)
        {
            preference = new UserSitePreference
            {
                SessionId = sessionId,
                PreferredSite = request.PreferredSite,
                LastDetectedDevice = detection.DeviceName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Set<UserSitePreference>().Add(preference);
        }
        else
        {
            preference.PreferredSite = request.PreferredSite;
            preference.LastDetectedDevice = detection.DeviceName;
            preference.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Saved site preference for anonymous session {SessionId}: {Preference}", sessionId, request.PreferredSite);

        return preference;
    }

    public string GetRecommendedSite(DeviceDetectionResult detection, int? screenWidth = null)
    {
        // If screen width is provided and below breakpoint, recommend mobile
        if (screenWidth.HasValue && screenWidth.Value < MobileBreakpoint)
        {
            return "mobile";
        }

        // Use detection result
        return detection.RecommendedSite;
    }

    private string DetectBrowser(string userAgent)
    {
        if (EdgeRegex.IsMatch(userAgent)) return "Edge";
        if (OperaRegex.IsMatch(userAgent)) return "Opera";
        if (ChromeRegex.IsMatch(userAgent)) return "Chrome";
        if (FirefoxRegex.IsMatch(userAgent)) return "Firefox";
        if (SafariRegex.IsMatch(userAgent)) return "Safari";
        return "Unknown";
    }

    private string DetectOperatingSystem(string userAgent)
    {
        var iosMatch = IOSRegex.Match(userAgent);
        if (iosMatch.Success)
        {
            var version = iosMatch.Groups[1].Success ? iosMatch.Groups[1].Value : iosMatch.Groups[2].Value;
            return $"iOS {version.Replace("_", ".")}";
        }

        var androidMatch = AndroidOSRegex.Match(userAgent);
        if (androidMatch.Success)
        {
            return $"Android {androidMatch.Groups[1].Value}";
        }

        var windowsMatch = WindowsRegex.Match(userAgent);
        if (windowsMatch.Success)
        {
            return $"Windows {MapWindowsVersion(windowsMatch.Groups[1].Value)}";
        }

        var macMatch = MacOSRegex.Match(userAgent);
        if (macMatch.Success)
        {
            return $"macOS {macMatch.Groups[1].Value.Replace("_", ".")}";
        }

        if (LinuxRegex.IsMatch(userAgent))
        {
            return "Linux";
        }

        return "Unknown";
    }

    private string DetectMobileDeviceName(string userAgent)
    {
        if (IPhoneRegex.IsMatch(userAgent)) return "iPhone";
        // Check Windows Phone before Android, as Windows Phone UAs may contain "Android"
        if (WindowsPhoneRegex.IsMatch(userAgent)) return "Windows Phone";
        if (AndroidPhoneRegex.IsMatch(userAgent)) return "Android Phone";
        if (userAgent.Contains("BlackBerry", StringComparison.OrdinalIgnoreCase)) return "BlackBerry";
        return "Mobile Device";
    }

    private string DetectTabletName(string userAgent)
    {
        if (IPadRegex.IsMatch(userAgent)) return "iPad";
        if (AndroidTabletRegex.IsMatch(userAgent)) return "Android Tablet";
        if (userAgent.Contains("Kindle", StringComparison.OrdinalIgnoreCase)) return "Kindle";
        if (userAgent.Contains("Silk", StringComparison.OrdinalIgnoreCase)) return "Amazon Fire";
        return "Tablet";
    }

    private string MapWindowsVersion(string ntVersion)
    {
        return ntVersion switch
        {
            "10.0" => "10/11",
            "6.3" => "8.1",
            "6.2" => "8",
            "6.1" => "7",
            "6.0" => "Vista",
            "5.1" => "XP",
            _ => ntVersion
        };
    }

    private void CacheResult(string key, DeviceDetectionResult result)
    {
        _cache.Set(key, result, CacheDuration);
    }

    private static string ComputeHash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
