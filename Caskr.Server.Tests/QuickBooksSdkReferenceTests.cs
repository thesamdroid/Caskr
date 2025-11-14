using Intuit.Ipp.OAuth2PlatformClient;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksSdkReferenceTests
{
    [Fact]
    public void QuickBooksSdkType_IsAccessible()
    {
        var type = typeof(OAuth2Client);

        Assert.Equal("Intuit.Ipp.OAuth2PlatformClient.OAuth2Client", type.FullName);
    }
}
