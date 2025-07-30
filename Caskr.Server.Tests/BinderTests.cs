using Caskr.server;
using Caskr.server.Repos;
using Caskr.server.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Caskr.Server.Tests;

public class BinderTests
{
    [Fact]
    public void BindServicesRegistersTypicalDependencies()
    {
        var services = new ServiceCollection();
        services.BindServices(null);
        var provider = services.BuildServiceProvider();

        var usersService = provider.GetService<IUsersService>();
        var usersRepo = provider.GetService<IUsersRepository>();

        Assert.NotNull(usersService);
        Assert.NotNull(usersRepo);
    }
}
