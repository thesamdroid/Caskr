using System.Diagnostics.CodeAnalysis;
using Caskr.server.Configuration;
using Caskr.server.Middleware;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Caskr.Server.Tests.Middleware;

public class MobileRedirectMiddlewareTests
{
    private readonly Mock<ILogger<MobileRedirectMiddleware>> _loggerMock = new();
    private readonly Mock<IMobileDetectionService> _mobileDetectionServiceMock = new();

    private const string MobileDomain = "m.caskr.co";
    private const string DesktopDomain = "caskr.co";
    private const string IPhoneUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15";
    private const string ChromeDesktopUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0";
    private const string GooglebotUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

    private MobileRedirectOptions CreateDefaultOptions(bool enabled = true)
    {
        return new MobileRedirectOptions
        {
            Enabled = enabled,
            MobileDomain = MobileDomain,
            DesktopDomain = DesktopDomain,
            RedirectStatusCode = 302,
            MobileBypassParameter = "nomobile",
            DesktopBypassParameter = "nodesktop",
            ExcludedPaths = new List<string> { "/api/", "/health", "/.well-known/", "/signin-oidc" },
            ExcludedExtensions = new List<string> { ".js", ".css", ".png", ".jpg", ".svg", ".woff" }
        };
    }

    private MobileRedirectMiddleware CreateMiddleware(
        RequestDelegate next,
        MobileRedirectOptions options)
    {
        var optionsWrapper = Options.Create(options);
        return new MobileRedirectMiddleware(next, optionsWrapper, _loggerMock.Object);
    }

    private DefaultHttpContext CreateHttpContext(
        string host,
        string path = "/",
        string queryString = "",
        string userAgent = ChromeDesktopUserAgent,
        string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        context.Request.Method = method;
        context.Request.Headers.UserAgent = userAgent;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private void SetupMobileDetection(DeviceType deviceType, bool isMobile, bool isTablet, bool isBot, string recommendedSite)
    {
        _mobileDetectionServiceMock
            .Setup(s => s.DetectDevice(It.IsAny<string>()))
            .Returns(new DeviceDetectionResult
            {
                DeviceType = deviceType,
                IsMobile = isMobile,
                IsTablet = isTablet,
                IsBot = isBot,
                RecommendedSite = recommendedSite
            });
    }

    #region Success Scenario Tests - Redirects

    [Fact]
    public async Task InvokeAsync_MobileUserAgentOnDesktopDomain_RedirectsToMobile()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/dashboard", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.False(nextCalled); // Next middleware should not be called
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains(MobileDomain, context.Response.Headers.Location.ToString());
        Assert.Contains("/dashboard", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_DesktopUserAgentOnMobileDomain_RedirectsToDesktop()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(MobileDomain, "/barrels", "", ChromeDesktopUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Desktop, false, false, false, "desktop");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains(DesktopDomain, context.Response.Headers.Location.ToString());
        Assert.Contains("/barrels", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_QueryParametersPreserved_DuringRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/orders", "?status=pending&page=2", IPhoneUserAgent);

        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        var location = context.Response.Headers.Location.ToString();
        Assert.Contains("status=pending", location);
        Assert.Contains("page=2", location);
    }

    [Fact]
    public async Task InvokeAsync_HttpsSchemePreserved_DuringRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/", "", IPhoneUserAgent);

        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        var location = context.Response.Headers.Location.ToString();
        Assert.StartsWith("https://", location);
    }

    [Fact]
    public async Task InvokeAsync_PermanentRedirect_WhenConfiguredAs301()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.RedirectStatusCode = 301;
        var context = CreateHttpContext(DesktopDomain, "/", "", IPhoneUserAgent);

        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.Equal(301, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_TabletOnDesktopDomain_RedirectsToMobile()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/tasks", "", "iPad User Agent");

        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);
        SetupMobileDetection(DeviceType.Tablet, false, true, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains(MobileDomain, context.Response.Headers.Location.ToString());
    }

    #endregion

    #region Skip Redirect Tests

    [Fact]
    public async Task InvokeAsync_ApiPath_NeverRedirected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/api/orders", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.NotEqual(302, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/mobile/detect")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/orders/123")]
    public async Task InvokeAsync_AllApiPaths_NeverRedirected(string path)
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, path, "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("/bundle.js")]
    [InlineData("/styles.css")]
    [InlineData("/logo.png")]
    [InlineData("/favicon.ico")]
    [InlineData("/fonts/roboto.woff2")]
    public async Task InvokeAsync_StaticFiles_NeverRedirected(string path)
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, path, "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_HealthEndpoint_NeverRedirected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/health", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_OAuthCallback_NeverRedirected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/signin-oidc", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_UserPreferenceDesktop_NotRedirectedFromDesktop()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/dashboard", "", IPhoneUserAgent);
        context.Request.Cookies = new MockCookieCollection(new Dictionary<string, string>
        {
            { "caskr_site_pref", "desktop" }
        });
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled); // Should continue, not redirect
    }

    [Fact]
    public async Task InvokeAsync_BypassParameterNoMobile_NotRedirectedAndSetsCookie()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/", "?nomobile=1", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.Contains(context.Response.Headers.SetCookie!, c => c!.Contains("caskr_site_pref=desktop"));
    }

    [Fact]
    public async Task InvokeAsync_BypassParameterNoDesktop_NotRedirectedAndSetsCookie()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(MobileDomain, "/", "?nodesktop=1", ChromeDesktopUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.Contains(context.Response.Headers.SetCookie!, c => c!.Contains("caskr_site_pref=mobile"));
    }

    [Fact]
    public async Task InvokeAsync_BotUserAgent_NeverRedirected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/", "", GooglebotUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Bot, false, false, true, "desktop");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsDisabled_PassesThrough()
    {
        // Arrange
        var options = CreateDefaultOptions(enabled: false);
        var context = CreateHttpContext(DesktopDomain, "/", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_PostRequest_NeverRedirected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/api/login", "", IPhoneUserAgent, "POST");
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    #endregion

    #region Failure Scenario Tests

    [Fact]
    public async Task InvokeAsync_DetectionServiceThrows_ContinuesWithoutRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/dashboard", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        _mobileDetectionServiceMock
            .Setup(s => s.DetectDevice(It.IsAny<string>()))
            .Throws(new Exception("Detection service error"));

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled); // Should continue despite error
    }

    [Fact]
    public void Constructor_NullMobileDomainWhenEnabled_ThrowsAtStartup()
    {
        // Arrange
        var options = new MobileRedirectOptions
        {
            Enabled = true,
            MobileDomain = null!,
            DesktopDomain = DesktopDomain
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            CreateMiddleware(_ => Task.CompletedTask, options));
    }

    [Fact]
    public void Constructor_NullDesktopDomainWhenEnabled_ThrowsAtStartup()
    {
        // Arrange
        var options = new MobileRedirectOptions
        {
            Enabled = true,
            MobileDomain = MobileDomain,
            DesktopDomain = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            CreateMiddleware(_ => Task.CompletedTask, options));
    }

    [Fact]
    public void Constructor_InvalidStatusCode_ThrowsAtStartup()
    {
        // Arrange
        var options = new MobileRedirectOptions
        {
            Enabled = true,
            MobileDomain = MobileDomain,
            DesktopDomain = DesktopDomain,
            RedirectStatusCode = 404 // Invalid
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            CreateMiddleware(_ => Task.CompletedTask, options));
    }

    [Fact]
    public async Task InvokeAsync_UnknownHost_DoesNotRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext("localhost:5000", "/", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled); // Should continue, unknown host
    }

    #endregion

    #region Same Site Tests

    [Fact]
    public async Task InvokeAsync_MobileUserAgentAlreadyOnMobile_NoRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(MobileDomain, "/dashboard", "", IPhoneUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Mobile, true, false, false, "mobile");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled); // Should continue without redirect
    }

    [Fact]
    public async Task InvokeAsync_DesktopUserAgentAlreadyOnDesktop_NoRedirect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var context = CreateHttpContext(DesktopDomain, "/", "", ChromeDesktopUserAgent);
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        SetupMobileDetection(DeviceType.Desktop, false, false, false, "desktop");

        // Act
        await middleware.InvokeAsync(context, _mobileDetectionServiceMock.Object);

        // Assert
        Assert.True(nextCalled);
    }

    #endregion
}

/// <summary>
/// Mock cookie collection for testing
/// </summary>
internal class MockCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public MockCookieCollection(Dictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
    public int Count => _cookies.Count;
    public ICollection<string> Keys => _cookies.Keys;
    public bool ContainsKey(string key) => _cookies.ContainsKey(key);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        var result = _cookies.TryGetValue(key, out var v);
        value = v;
        return result;
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
}
