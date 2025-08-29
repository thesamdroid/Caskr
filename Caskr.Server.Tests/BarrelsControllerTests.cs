using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public class BarrelsControllerTests
{
    private readonly Mock<IBarrelsService> _barrelsService = new();
    private readonly Mock<IUsersService> _usersService = new();

    private BarrelsController CreateController(User user)
    {
        var controller = new BarrelsController(_barrelsService.Object, _usersService.Object);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        }, "test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _usersService.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

        return controller;
    }

    [Fact]
    public async Task GetBarrelsForCompany_NonAdminDifferentCompany_ReturnsForbid()
    {
        var user = new User { Id = 1, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Distiller };
        var controller = CreateController(user);

        var result = await controller.GetBarrelsForCompany(2);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetBarrelsForCompany_AdminAnyCompany_ReturnsOk()
    {
        var user = new User { Id = 2, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _barrelsService.Setup(s => s.GetBarrelsForCompanyAsync(3)).ReturnsAsync(new[] { new Barrel { Id = 10 } });

        var result = await controller.GetBarrelsForCompany(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var barrels = Assert.IsAssignableFrom<IEnumerable<Barrel>>(ok.Value);
        Assert.Single(barrels);
    }
}

