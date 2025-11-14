using System;
using System.Collections.Generic;
using System.Security.Claims;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.server;
using Caskr.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public class QuickBooksControllerTests : IDisposable
{
    private readonly Mock<IQuickBooksAuthService> _authService = new();
    private readonly Mock<IUsersService> _usersService = new();
    private readonly IConfiguration _configuration;
    private CaskrDbContext? _context;

    public QuickBooksControllerTests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["QuickBooks:ConnectSuccessRedirectUrl"] = "https://app.caskr.dev/accounting/quickbooks/success"
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    [Fact]
    public async Task Connect_WithValidRequest_ReturnsAuthorizationUrl()
    {
        var user = new User { Id = 100, CompanyId = 77, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        var authorizationUri = new Uri("https://quickbooks.example.com/authorize");
        _authService
            .Setup(s => s.GetAuthorizationUrlAsync(77, It.IsAny<string>()))
            .ReturnsAsync(authorizationUri);

        var result = await controller.Connect(new QuickBooksCompanyRequest { CompanyId = 77 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksAuthUrlResponse>(ok.Value);
        Assert.Equal(authorizationUri.ToString(), payload.AuthUrl);
    }

    [Fact]
    public async Task Disconnect_UserWithoutCompanyAccess_ReturnsForbid()
    {
        var user = new User { Id = 200, CompanyId = 10, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var response = await controller.Disconnect(new QuickBooksCompanyRequest { CompanyId = 11 });

        Assert.IsType<ForbidResult>(response);
        _authService.Verify(s => s.RevokeAccessAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Status_NoIntegration_ReturnsDisconnectedPayload()
    {
        var user = new User { Id = 300, CompanyId = 21, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var result = await controller.GetStatus(21);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksStatusResponse>(ok.Value);
        Assert.False(payload.Connected);
        Assert.Null(payload.RealmId);
        Assert.Null(payload.ConnectedAt);
    }

    private QuickBooksController CreateController(User user)
    {
        _context = new CaskrDbContext(new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var controller = new QuickBooksController(
            _authService.Object,
            _usersService.Object,
            _context,
            NullLogger<QuickBooksController>.Instance,
            _configuration);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }, "test"))
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.caskr.dev");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _usersService.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        return controller;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
