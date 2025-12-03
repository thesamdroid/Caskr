using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Services;

public class MobileDetectionServiceTests
{
    private readonly Mock<ILogger<MobileDetectionService>> _loggerMock = new();

    #region Test User-Agent Strings

    // Mobile User-Agent strings
    private const string IPhoneUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
    private const string IPhone15ProUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1.2 Mobile/15E148 Safari/604.1";
    private const string AndroidPhoneUserAgent = "Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.43 Mobile Safari/537.36";
    private const string Pixel7UserAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 7 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.43 Mobile Safari/537.36";
    private const string OnePlusUserAgent = "Mozilla/5.0 (Linux; Android 14; CPH2449) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.6045.163 Mobile Safari/537.36";
    private const string WindowsPhoneUserAgent = "Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.14977";
    private const string BlackBerryUserAgent = "Mozilla/5.0 (BB10; Kbd) AppleWebKit/537.35+ (KHTML, like Gecko) Version/10.3.3.2205 Mobile Safari/537.35+";
    private const string SamsungGalaxyUserAgent = "Mozilla/5.0 (Linux; Android 13; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.6045.163 Mobile Safari/537.36";
    private const string XiaomiUserAgent = "Mozilla/5.0 (Linux; Android 13; 2203121C) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.6045.163 Mobile Safari/537.36";
    private const string HuaweiUserAgent = "Mozilla/5.0 (Linux; Android 12; NOH-AN00) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.88 Mobile Safari/537.36";

    // Tablet User-Agent strings
    private const string IPadUserAgent = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
    private const string IPadProUserAgent = "Mozilla/5.0 (iPad; CPU OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/120.0.6099.101 Mobile/15E148 Safari/604.1";
    private const string AndroidTabletUserAgent = "Mozilla/5.0 (Linux; Android 13; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.43 Safari/537.36";
    private const string KindleFireUserAgent = "Mozilla/5.0 (Linux; Android 9; KFTRWI) AppleWebKit/537.36 (KHTML, like Gecko) Silk/123.3.1 like Chrome/120.0.6099.43 Safari/537.36";
    private const string GalaxyTabUserAgent = "Mozilla/5.0 (Linux; Android 13; SM-T870) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.6045.163 Safari/537.36";

    // Desktop User-Agent strings
    private const string ChromeWindowsUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private const string FirefoxWindowsUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0";
    private const string SafariMacUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15";
    private const string EdgeWindowsUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.2210.61";
    private const string ChromeLinuxUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    // Bot/Crawler User-Agent strings
    private const string GooglebotUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
    private const string BingbotUserAgent = "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)";
    private const string SlackbotUserAgent = "Slackbot-LinkExpanding 1.0 (+https://api.slack.com/robots)";
    private const string FacebookCrawlerUserAgent = "facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)";
    private const string TwitterbotUserAgent = "Twitterbot/1.0";
    private const string CurlUserAgent = "curl/7.88.1";
    private const string PythonRequestsUserAgent = "python-requests/2.28.1";

    #endregion

    private CaskrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private MobileDetectionService CreateService(CaskrDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new MobileDetectionService(context, memoryCache, _loggerMock.Object);
    }

    #region Mobile Device Detection Tests

    [Theory]
    [InlineData(IPhoneUserAgent, "iPhone")]
    [InlineData(IPhone15ProUserAgent, "iPhone")]
    [InlineData(AndroidPhoneUserAgent, "Android Phone")]
    [InlineData(Pixel7UserAgent, "Android Phone")]
    [InlineData(OnePlusUserAgent, "Android Phone")]
    [InlineData(WindowsPhoneUserAgent, "Windows Phone")]
    [InlineData(SamsungGalaxyUserAgent, "Android Phone")]
    [InlineData(XiaomiUserAgent, "Android Phone")]
    [InlineData(HuaweiUserAgent, "Android Phone")]
    public void DetectDevice_MobileUserAgent_ReturnsCorrectDeviceType(string userAgent, string expectedDeviceName)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(userAgent);

        // Assert
        Assert.Equal(DeviceType.Mobile, result.DeviceType);
        Assert.True(result.IsMobile);
        Assert.False(result.IsTablet);
        Assert.False(result.IsBot);
        Assert.True(result.HasTouchCapability);
        Assert.Equal("mobile", result.RecommendedSite);
        Assert.Equal(expectedDeviceName, result.DeviceName);
    }

    [Theory]
    [InlineData(BlackBerryUserAgent)]
    public void DetectDevice_BlackBerryUserAgent_DetectsAsMobile(string userAgent)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(userAgent);

        // Assert
        Assert.Equal(DeviceType.Mobile, result.DeviceType);
        Assert.True(result.IsMobile);
        Assert.Equal("mobile", result.RecommendedSite);
    }

    #endregion

    #region Tablet Detection Tests

    [Theory]
    [InlineData(IPadUserAgent, "iPad")]
    [InlineData(IPadProUserAgent, "iPad")]
    [InlineData(AndroidTabletUserAgent, "Android Tablet")]
    [InlineData(GalaxyTabUserAgent, "Android Tablet")]
    public void DetectDevice_TabletUserAgent_ReturnsTabletAndMobileSite(string userAgent, string expectedDeviceName)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(userAgent);

        // Assert
        Assert.Equal(DeviceType.Tablet, result.DeviceType);
        Assert.True(result.IsTablet);
        Assert.False(result.IsMobile);
        Assert.False(result.IsBot);
        Assert.True(result.HasTouchCapability);
        Assert.Equal("mobile", result.RecommendedSite); // Tablets should go to mobile site
        Assert.Equal(expectedDeviceName, result.DeviceName);
    }

    [Fact]
    public void DetectDevice_KindleFire_DetectsAsTablet()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(KindleFireUserAgent);

        // Assert
        Assert.Equal(DeviceType.Tablet, result.DeviceType);
        Assert.True(result.IsTablet);
        Assert.Equal("mobile", result.RecommendedSite);
    }

    #endregion

    #region Desktop Detection Tests

    [Theory]
    [InlineData(ChromeWindowsUserAgent, "Chrome")]
    [InlineData(FirefoxWindowsUserAgent, "Firefox")]
    [InlineData(SafariMacUserAgent, "Safari")]
    [InlineData(EdgeWindowsUserAgent, "Edge")]
    [InlineData(ChromeLinuxUserAgent, "Chrome")]
    public void DetectDevice_DesktopUserAgent_ReturnsDesktopDevice(string userAgent, string expectedBrowser)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(userAgent);

        // Assert
        Assert.Equal(DeviceType.Desktop, result.DeviceType);
        Assert.False(result.IsMobile);
        Assert.False(result.IsTablet);
        Assert.False(result.IsBot);
        Assert.Equal("desktop", result.RecommendedSite);
        Assert.Equal(expectedBrowser, result.Browser);
    }

    [Fact]
    public void DetectDevice_ChromeWindows_DetectsWindowsOS()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(ChromeWindowsUserAgent);

        // Assert
        Assert.Contains("Windows", result.OperatingSystem);
    }

    [Fact]
    public void DetectDevice_SafariMac_DetectsMacOS()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(SafariMacUserAgent);

        // Assert
        Assert.Contains("macOS", result.OperatingSystem);
    }

    #endregion

    #region Bot Detection Tests

    [Theory]
    [InlineData(GooglebotUserAgent)]
    [InlineData(BingbotUserAgent)]
    [InlineData(SlackbotUserAgent)]
    [InlineData(FacebookCrawlerUserAgent)]
    [InlineData(TwitterbotUserAgent)]
    [InlineData(CurlUserAgent)]
    [InlineData(PythonRequestsUserAgent)]
    public void DetectDevice_BotUserAgent_ReturnsBot(string userAgent)
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(userAgent);

        // Assert
        Assert.Equal(DeviceType.Bot, result.DeviceType);
        Assert.True(result.IsBot);
        Assert.False(result.IsMobile);
        Assert.False(result.IsTablet);
        Assert.Equal("desktop", result.RecommendedSite); // Bots should not redirect
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void DetectDevice_EmptyUserAgent_ReturnsUnknown()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(string.Empty);

        // Assert
        Assert.Equal(DeviceType.Unknown, result.DeviceType);
        Assert.False(result.IsMobile);
        Assert.False(result.IsTablet);
        Assert.False(result.IsBot);
        Assert.Equal("desktop", result.RecommendedSite);
    }

    [Fact]
    public void DetectDevice_NullUserAgent_ReturnsUnknown()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(null);

        // Assert
        Assert.Equal(DeviceType.Unknown, result.DeviceType);
        Assert.Equal("desktop", result.RecommendedSite);
    }

    [Fact]
    public void DetectDevice_MalformedUserAgent_HandlesGracefully()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var malformedUserAgent = "!@#$%^&*()_+{}|:<>?[]\\;',./`~";

        // Act
        var result = service.DetectDevice(malformedUserAgent);

        // Assert - should not throw and return unknown
        Assert.NotNull(result);
        Assert.Equal(DeviceType.Unknown, result.DeviceType);
    }

    [Fact]
    public void DetectDevice_VeryLongUserAgent_TruncatesAndHandles()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var longUserAgent = new string('A', 5000) + " iPhone Mobile";

        // Act
        var result = service.DetectDevice(longUserAgent);

        // Assert - should not throw and handle gracefully
        Assert.NotNull(result);
        // User-Agent is truncated so iPhone pattern is cut off
        Assert.Equal(DeviceType.Unknown, result.DeviceType);
    }

    [Fact]
    public void DetectDevice_WhitespaceOnlyUserAgent_ReturnsUnknown()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice("   \t\n  ");

        // Assert
        Assert.Equal(DeviceType.Unknown, result.DeviceType);
    }

    #endregion

    #region Redirect Logic Tests

    [Fact]
    public void ShouldRedirectToMobile_MobileUserAgent_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(IPhoneUserAgent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRedirectToMobile_DesktopUserAgent_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(ChromeWindowsUserAgent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirectToMobile_TabletUserAgent_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(IPadUserAgent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRedirectToMobile_BotUserAgent_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(GooglebotUserAgent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirectToMobile_UserPrefersDesktop_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(IPhoneUserAgent, SitePreference.Desktop);

        // Assert
        Assert.False(result); // User preference overrides detection
    }

    [Fact]
    public void ShouldRedirectToMobile_UserPrefersMobile_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToMobile(ChromeWindowsUserAgent, SitePreference.Mobile);

        // Assert
        Assert.True(result); // User preference overrides detection
    }

    [Fact]
    public void ShouldRedirectToDesktop_DesktopUserAgent_ReturnsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToDesktop(ChromeWindowsUserAgent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRedirectToDesktop_MobileUserAgent_ReturnsFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.ShouldRedirectToDesktop(IPhoneUserAgent);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Preference Storage Tests

    [Fact]
    public async Task SavePreferenceAsync_AuthenticatedUser_SavesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com", UserTypeId = 1, CompanyId = 1 };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new SavePreferenceRequest { PreferredSite = SitePreference.Mobile };

        // Act
        var result = await service.SavePreferenceAsync(1, request, IPhoneUserAgent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(SitePreference.Mobile, result.PreferredSite);
        Assert.Equal("iPhone", result.LastDetectedDevice);
    }

    [Fact]
    public async Task SavePreferenceAsync_UpdatesExistingPreference()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com", UserTypeId = 1, CompanyId = 1 };
        context.Users.Add(user);

        var existingPreference = new UserSitePreference
        {
            UserId = 1,
            PreferredSite = SitePreference.Desktop,
            LastDetectedDevice = "Desktop",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Set<UserSitePreference>().Add(existingPreference);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new SavePreferenceRequest { PreferredSite = SitePreference.Mobile };

        // Act
        var result = await service.SavePreferenceAsync(1, request, IPhoneUserAgent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SitePreference.Mobile, result.PreferredSite);
        Assert.Equal("iPhone", result.LastDetectedDevice);

        // Verify only one preference exists for user
        var count = await context.Set<UserSitePreference>().CountAsync(p => p.UserId == 1);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAnonymousPreferenceAsync_CreatesNewPreference()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var sessionId = Guid.NewGuid().ToString();
        var request = new SavePreferenceRequest { PreferredSite = SitePreference.Desktop };

        // Act
        var result = await service.SaveAnonymousPreferenceAsync(sessionId, request, ChromeWindowsUserAgent);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.UserId);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(SitePreference.Desktop, result.PreferredSite);
    }

    [Fact]
    public async Task SaveAnonymousPreferenceAsync_EmptySessionId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new SavePreferenceRequest { PreferredSite = SitePreference.Mobile };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAnonymousPreferenceAsync(string.Empty, request, IPhoneUserAgent));
    }

    [Fact]
    public async Task SaveAnonymousPreferenceAsync_WhitespaceSessionId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new SavePreferenceRequest { PreferredSite = SitePreference.Mobile };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAnonymousPreferenceAsync("   ", request, IPhoneUserAgent));
    }

    [Fact]
    public async Task GetPreferenceByUserIdAsync_ExistingUser_ReturnsPreference()
    {
        // Arrange
        using var context = CreateDbContext();
        var preference = new UserSitePreference
        {
            UserId = 1,
            PreferredSite = SitePreference.Mobile,
            LastDetectedDevice = "iPhone"
        };
        context.Set<UserSitePreference>().Add(preference);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetPreferenceByUserIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SitePreference.Mobile, result.PreferredSite);
    }

    [Fact]
    public async Task GetPreferenceByUserIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetPreferenceByUserIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPreferenceBySessionIdAsync_ExistingSession_ReturnsPreference()
    {
        // Arrange
        using var context = CreateDbContext();
        var sessionId = Guid.NewGuid().ToString();
        var preference = new UserSitePreference
        {
            SessionId = sessionId,
            PreferredSite = SitePreference.Desktop,
            LastDetectedDevice = "Desktop"
        };
        context.Set<UserSitePreference>().Add(preference);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetPreferenceBySessionIdAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SitePreference.Desktop, result.PreferredSite);
    }

    [Fact]
    public async Task GetPreferenceBySessionIdAsync_EmptySessionId_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetPreferenceBySessionIdAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Recommended Site Tests

    [Fact]
    public void GetRecommendedSite_SmallScreenWidth_ReturnsMobile()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var detection = new DeviceDetectionResult
        {
            DeviceType = DeviceType.Desktop,
            RecommendedSite = "desktop"
        };

        // Act
        var result = service.GetRecommendedSite(detection, 600); // Below 768px breakpoint

        // Assert
        Assert.Equal("mobile", result);
    }

    [Fact]
    public void GetRecommendedSite_LargeScreenWidth_UsesDetectionResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var detection = new DeviceDetectionResult
        {
            DeviceType = DeviceType.Desktop,
            RecommendedSite = "desktop"
        };

        // Act
        var result = service.GetRecommendedSite(detection, 1920);

        // Assert
        Assert.Equal("desktop", result);
    }

    [Fact]
    public void GetRecommendedSite_NoScreenWidth_UsesDetectionResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var detection = new DeviceDetectionResult
        {
            DeviceType = DeviceType.Mobile,
            IsMobile = true,
            RecommendedSite = "mobile"
        };

        // Act
        var result = service.GetRecommendedSite(detection);

        // Assert
        Assert.Equal("mobile", result);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void DetectDevice_SameUserAgent_ReturnsCachedResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act - First call
        var result1 = service.DetectDevice(IPhoneUserAgent);

        // Act - Second call should use cache
        var result2 = service.DetectDevice(IPhoneUserAgent);

        // Assert - Results should be identical (cached)
        Assert.Equal(result1.DeviceType, result2.DeviceType);
        Assert.Equal(result1.DeviceName, result2.DeviceName);
        Assert.Equal(result1.RecommendedSite, result2.RecommendedSite);
    }

    #endregion

    #region OS Detection Tests

    [Fact]
    public void DetectDevice_IPhone_DetectsIOS()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(IPhoneUserAgent);

        // Assert
        Assert.Contains("iOS", result.OperatingSystem);
    }

    [Fact]
    public void DetectDevice_AndroidPhone_DetectsAndroid()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(AndroidPhoneUserAgent);

        // Assert
        Assert.Contains("Android", result.OperatingSystem);
    }

    [Fact]
    public void DetectDevice_LinuxDesktop_DetectsLinux()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = service.DetectDevice(ChromeLinuxUserAgent);

        // Assert
        Assert.Equal("Linux", result.OperatingSystem);
    }

    #endregion
}
