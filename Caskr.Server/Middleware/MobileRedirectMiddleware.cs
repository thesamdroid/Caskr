using Caskr.server.Configuration;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.Extensions.Options;

namespace Caskr.server.Middleware;

/// <summary>
/// Middleware that redirects users between desktop and mobile sites based on device detection
/// </summary>
public class MobileRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MobileRedirectOptions _options;
    private readonly ILogger<MobileRedirectMiddleware> _logger;

    public MobileRedirectMiddleware(
        RequestDelegate next,
        IOptions<MobileRedirectOptions> options,
        ILogger<MobileRedirectMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;

        // Validate configuration at startup
        _options.Validate();
    }

    public async Task InvokeAsync(HttpContext context, IMobileDetectionService mobileDetectionService)
    {
        // Skip if redirects are disabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        try
        {
            // Check if this request should skip redirect
            if (ShouldSkipRedirect(context))
            {
                await _next(context);
                return;
            }

            // Check for bypass parameters
            var bypassResult = CheckBypassParameters(context);
            if (bypassResult.HasBypass)
            {
                // Set preference cookie and continue without redirect
                SetPreferenceCookie(context, bypassResult.Preference);
                await _next(context);
                return;
            }

            // Get user preference from cookie
            var userPreference = GetPreferenceFromCookie(context);

            // Get User-Agent and detect device
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var detection = mobileDetectionService.DetectDevice(userAgent);

            // Determine if redirect is needed
            var redirectResult = DetermineRedirect(context, detection, userPreference);

            if (redirectResult.ShouldRedirect)
            {
                var redirectUrl = BuildRedirectUrl(context, redirectResult.TargetDomain);

                _logger.LogInformation(
                    "Redirecting {DeviceType} device from {CurrentHost} to {TargetDomain}. Path: {Path}",
                    detection.DeviceType,
                    context.Request.Host.Value,
                    redirectResult.TargetDomain,
                    context.Request.Path);

                context.Response.Redirect(redirectUrl, _options.RedirectStatusCode == 301);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            // Log error but don't prevent request from completing
            _logger.LogError(ex, "Error in mobile redirect middleware, continuing without redirect");
            await _next(context);
        }
    }

    /// <summary>
    /// Determines if the request should skip redirect processing
    /// </summary>
    private bool ShouldSkipRedirect(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip excluded paths
        foreach (var excludedPath in _options.ExcludedPaths)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Skip excluded file extensions
        foreach (var extension in _options.ExcludedExtensions)
        {
            if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Skip non-GET requests (POST, PUT, DELETE, etc.)
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for bypass parameters in the query string
    /// </summary>
    private (bool HasBypass, SitePreference Preference) CheckBypassParameters(HttpContext context)
    {
        var query = context.Request.Query;

        // Check for nomobile parameter (user wants to stay on desktop)
        if (query.ContainsKey(_options.MobileBypassParameter))
        {
            var value = query[_options.MobileBypassParameter].FirstOrDefault();
            if (value == "1" || value?.ToLowerInvariant() == "true")
            {
                return (true, SitePreference.Desktop);
            }
        }

        // Check for nodesktop parameter (user wants to stay on mobile)
        if (query.ContainsKey(_options.DesktopBypassParameter))
        {
            var value = query[_options.DesktopBypassParameter].FirstOrDefault();
            if (value == "1" || value?.ToLowerInvariant() == "true")
            {
                return (true, SitePreference.Mobile);
            }
        }

        return (false, SitePreference.Auto);
    }

    /// <summary>
    /// Gets user preference from cookie
    /// </summary>
    private SitePreference? GetPreferenceFromCookie(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(_options.PreferenceCookieName, out var value))
        {
            return value?.ToLowerInvariant() switch
            {
                "desktop" => SitePreference.Desktop,
                "mobile" => SitePreference.Mobile,
                "auto" => SitePreference.Auto,
                _ => null
            };
        }
        return null;
    }

    /// <summary>
    /// Sets user preference cookie
    /// </summary>
    private void SetPreferenceCookie(HttpContext context, SitePreference preference)
    {
        var cookieValue = preference switch
        {
            SitePreference.Desktop => "desktop",
            SitePreference.Mobile => "mobile",
            _ => "auto"
        };

        context.Response.Cookies.Append(_options.PreferenceCookieName, cookieValue, new CookieOptions
        {
            HttpOnly = false, // Allow JS access for client-side preference management
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(_options.PreferenceCookieExpirationDays)
        });
    }

    /// <summary>
    /// Determines if redirect is needed and to which domain
    /// </summary>
    private (bool ShouldRedirect, string TargetDomain) DetermineRedirect(
        HttpContext context,
        DeviceDetectionResult detection,
        SitePreference? userPreference)
    {
        var currentHost = context.Request.Host.Value.ToLowerInvariant();
        var isOnMobileSite = currentHost.Contains(_options.MobileDomain.ToLowerInvariant());
        var isOnDesktopSite = currentHost.Contains(_options.DesktopDomain.ToLowerInvariant()) && !isOnMobileSite;

        // If we can't determine current site, don't redirect
        if (!isOnMobileSite && !isOnDesktopSite)
        {
            return (false, string.Empty);
        }

        // Don't redirect bots
        if (detection.IsBot)
        {
            return (false, string.Empty);
        }

        // Determine target based on preference or detection
        string targetSite;

        if (userPreference == SitePreference.Desktop)
        {
            targetSite = "desktop";
        }
        else if (userPreference == SitePreference.Mobile)
        {
            targetSite = "mobile";
        }
        else
        {
            // Auto mode: use detection
            targetSite = detection.RecommendedSite;
        }

        // Determine if redirect is needed
        if (isOnDesktopSite && targetSite == "mobile")
        {
            return (true, _options.MobileDomain);
        }

        if (isOnMobileSite && targetSite == "desktop")
        {
            return (true, _options.DesktopDomain);
        }

        return (false, string.Empty);
    }

    /// <summary>
    /// Builds the redirect URL preserving path and query string
    /// </summary>
    private string BuildRedirectUrl(HttpContext context, string targetDomain)
    {
        var request = context.Request;
        var scheme = request.Scheme;
        var path = request.Path.Value ?? string.Empty;
        var queryString = request.QueryString.Value ?? string.Empty;

        // Remove bypass parameters from query string if present
        if (!string.IsNullOrEmpty(queryString))
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);
            query.Remove(_options.MobileBypassParameter);
            query.Remove(_options.DesktopBypassParameter);

            if (query.Count > 0)
            {
                queryString = "?" + string.Join("&", query.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}"));
            }
            else
            {
                queryString = string.Empty;
            }
        }

        return $"{scheme}://{targetDomain}{path}{queryString}";
    }
}
