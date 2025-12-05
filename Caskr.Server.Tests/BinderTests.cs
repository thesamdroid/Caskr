using Caskr.server;
using Caskr.server.Repos;
using Caskr.server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

namespace Caskr.Server.Tests;

public class BinderTests
{
    [Fact]
    public void BindServicesRegistersTypicalDependencies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        services.BindServices(configuration);
        var provider = services.BuildServiceProvider();

        var usersService = provider.GetService<IUsersService>();
        var usersRepo = provider.GetService<IUsersRepository>();

        Assert.NotNull(usersService);
        Assert.NotNull(usersRepo);
    }
}
