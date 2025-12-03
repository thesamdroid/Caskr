namespace Caskr.server.Configuration;

/// <summary>
/// Configuration options for mobile redirect middleware
/// </summary>
public class MobileRedirectOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "MobileRedirect";

    /// <summary>
    /// Master switch to enable/disable redirects
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The mobile site domain (e.g., m.caskr.co)
    /// </summary>
    public string MobileDomain { get; set; } = "m.caskr.co";

    /// <summary>
    /// The desktop site domain (e.g., caskr.co)
    /// </summary>
    public string DesktopDomain { get; set; } = "caskr.co";

    /// <summary>
    /// Paths to never redirect (always skip)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/api/",
        "/health",
        "/healthz",
        "/.well-known/",
        "/signin-oidc",
        "/signout-callback-oidc",
        "/auth/callback",
        "/oauth/callback"
    };

    /// <summary>
    /// File extensions to skip (static assets)
    /// </summary>
    public List<string> ExcludedExtensions { get; set; } = new()
    {
        ".js",
        ".css",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".ico",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".map",
        ".json",
        ".xml",
        ".txt",
        ".webp",
        ".mp4",
        ".webm",
        ".pdf"
    };

    /// <summary>
    /// Query parameter to bypass redirect (e.g., ?nomobile=1)
    /// </summary>
    public string MobileBypassParameter { get; set; } = "nomobile";

    /// <summary>
    /// Query parameter to force desktop on mobile domain (e.g., ?nodesktop=1)
    /// </summary>
    public string DesktopBypassParameter { get; set; } = "nodesktop";

    /// <summary>
    /// HTTP status code for redirect (301 permanent or 302 temporary)
    /// </summary>
    public int RedirectStatusCode { get; set; } = 302;

    /// <summary>
    /// Cookie name for storing user's site preference
    /// </summary>
    public string PreferenceCookieName { get; set; } = "caskr_site_pref";

    /// <summary>
    /// Cookie expiration in days
    /// </summary>
    public int PreferenceCookieExpirationDays { get; set; } = 365;

    /// <summary>
    /// Validates the configuration and throws if invalid
    /// </summary>
    public void Validate()
    {
        if (Enabled)
        {
            if (string.IsNullOrWhiteSpace(MobileDomain))
            {
                throw new InvalidOperationException("MobileRedirect:MobileDomain must be configured when redirects are enabled");
            }

            if (string.IsNullOrWhiteSpace(DesktopDomain))
            {
                throw new InvalidOperationException("MobileRedirect:DesktopDomain must be configured when redirects are enabled");
            }

            if (RedirectStatusCode != 301 && RedirectStatusCode != 302)
            {
                throw new InvalidOperationException("MobileRedirect:RedirectStatusCode must be either 301 or 302");
            }
        }
    }
}
