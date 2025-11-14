using System;
using System.Linq;
using System.Threading.Tasks;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksAuthServiceContractTests
{
    [Fact]
    public void Interface_ShouldExposeExpectedAsyncMethods()
    {
        var contract = typeof(IQuickBooksAuthService);
        Assert.True(contract.IsInterface);

        AssertMethod(contract, nameof(IQuickBooksAuthService.GetAuthorizationUrlAsync), typeof(Task<Uri>),
            typeof(int), typeof(string));
        AssertMethod(contract, nameof(IQuickBooksAuthService.HandleCallbackAsync), typeof(Task<OAuthTokenResponse>),
            typeof(string), typeof(string), typeof(int));
        AssertMethod(contract, nameof(IQuickBooksAuthService.RefreshTokenAsync), typeof(Task<OAuthTokenResponse>),
            typeof(int));
        AssertMethod(contract, nameof(IQuickBooksAuthService.RevokeAccessAsync), typeof(Task), typeof(int));
    }

    [Fact]
    public void OAuthTokenResponse_ShouldPersistValues()
    {
        var dto = new OAuthTokenResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresIn = 3600,
            RealmId = "realm"
        };

        Assert.Equal("access", dto.AccessToken);
        Assert.Equal("refresh", dto.RefreshToken);
        Assert.Equal(3600, dto.ExpiresIn);
        Assert.Equal("realm", dto.RealmId);
    }

    private static void AssertMethod(Type contract, string methodName, Type returnType, params Type[] parameters)
    {
        var method = contract.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Equal(returnType, method!.ReturnType);

        var methodParameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(parameters, methodParameters);
    }
}
