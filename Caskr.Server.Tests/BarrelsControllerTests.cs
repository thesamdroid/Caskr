using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IO;
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
    public async Task GetBarrelsForCompany_NonSuperAdminDifferentCompany_ReturnsForbid()
    {
        var user = new User { Id = 1, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Distiller };
        var controller = CreateController(user);

        var result = await controller.GetBarrelsForCompany(2);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetBarrelsForCompany_SuperAdminAnyCompany_ReturnsOk()
    {
        var user = new User { Id = 2, CompanyId = 1, UserTypeId = (int)UserTypeEnum.SuperAdmin };
        var controller = CreateController(user);
        _barrelsService.Setup(s => s.GetBarrelsForCompanyAsync(3)).ReturnsAsync(new[] { new Barrel { Id = 10 } });

        var result = await controller.GetBarrelsForCompany(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var barrels = Assert.IsAssignableFrom<IEnumerable<Barrel>>(ok.Value);
        Assert.Single(barrels);
    }

    [Fact]
    public async Task ImportBarrels_NoFile_ReturnsBadRequest()
    {
        var user = new User { Id = 3, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var response = await controller.ImportBarrels(1, new BarrelImportRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        dynamic payload = badRequest.Value!;
        Assert.Equal("Please select a CSV file to upload.", (string)payload.message);
    }

    [Fact]
    public async Task ImportBarrels_ServiceRequestsMashBill_ReturnsBadRequestWithFlag()
    {
        var user = new User { Id = 4, CompanyId = 1, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _barrelsService
            .Setup(s => s.ImportBarrelsAsync(1, user.Id, It.IsAny<IFormFile>(), null, null))
            .ThrowsAsync(new BatchRequiredException());

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(stream, 0, stream.Length, "file", "data.csv");
        var request = new BarrelImportRequest { File = file };

        var response = await controller.ImportBarrels(1, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        dynamic payload = badRequest.Value!;
        Assert.True((bool)payload.requiresMashBillId);
    }

    [Fact]
    public async Task ImportBarrels_Success_ReturnsOk()
    {
        var user = new User { Id = 5, CompanyId = 2, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _barrelsService
            .Setup(s => s.ImportBarrelsAsync(2, user.Id, It.IsAny<IFormFile>(), 1, null))
            .ReturnsAsync(new BarrelImportResult(1, 3, false));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(stream, 0, stream.Length, "file", "data.csv");
        var request = new BarrelImportRequest { File = file, BatchId = 1 };

        var response = await controller.ImportBarrels(2, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        dynamic payload = ok.Value!;
        Assert.Equal(3, (int)payload.created);
        Assert.Equal(1, (int)payload.batchId);
    }
}

