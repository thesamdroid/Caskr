using System.Security.Claims;
using System.Threading;
using Caskr.server;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public sealed class TtbReportsControllerTests : IDisposable
{
    private readonly Mock<ITtbReportCalculator> calculator = new();
    private readonly Mock<ITtbPdfGenerator> pdfGenerator = new();
    private readonly Mock<IUsersService> usersService = new();
    private readonly CaskrDbContext dbContext;

    public TtbReportsControllerTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbContext = new CaskrDbContext(options);
        dbContext.Companies.Add(new Company
        {
            Id = 10,
            CompanyName = "Heritage Spirits",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });

        dbContext.Users.Add(new User
        {
            Id = 25,
            CompanyId = 10,
            Email = "admin@heritage.test",
            Name = "Heritage Admin",
            UserTypeId = (int)UserTypeEnum.Admin,
            IsPrimaryContact = true
        });

        dbContext.Users.Add(new User
        {
            Id = 30,
            CompanyId = 22,
            Email = "other@company.test",
            Name = "Other User",
            UserTypeId = (int)UserTypeEnum.Distiller,
            IsPrimaryContact = false
        });

        dbContext.SaveChanges();
    }

    [Fact]
    public async Task Generate_WhenExistingSubmittedReport_ReturnsConflict()
    {
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 10,
            ReportMonth = 9,
            ReportYear = 2024,
            Status = TtbReportStatus.Submitted,
            GeneratedAt = DateTime.UtcNow,
            CreatedByUserId = 25
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);
        var result = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 9,
            Year = 2024
        });

        Assert.IsType<ConflictObjectResult>(result);
        calculator.Verify(c => c.CalculateMonthlyReportAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Generate_WhenUserLacksPermission_ReturnsForbid()
    {
        var controller = CreateController(30);

        var result = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 8,
            Year = 2024
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Generate_WithDraftReport_RegeneratesAndReturnsPdf()
    {
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 10,
            ReportMonth = 8,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            GeneratedAt = DateTime.UtcNow.AddDays(-1),
            PdfPath = "/tmp/old.pdf",
            CreatedByUserId = 25
        });

        await dbContext.SaveChangesAsync();

        var reportData = new TtbMonthlyReportData
        {
            CompanyId = 10,
            Month = 8,
            Year = 2024,
            OpeningInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 12m,
                        WineGallons = 6m
                    }
                }
            },
            Production = new ProductionSection { Rows = Array.Empty<TtbSectionTotal>() },
            Transfers = new TransfersSection
            {
                TransfersIn = Array.Empty<TtbSectionTotal>(),
                TransfersOut = Array.Empty<TtbSectionTotal>()
            },
            Losses = new LossSection { Rows = Array.Empty<TtbSectionTotal>() },
            ClosingInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 12m,
                        WineGallons = 6m
                    }
                }
            }
        };

        calculator.Setup(c => c.CalculateMonthlyReportAsync(10, 8, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportData);

        pdfGenerator.Setup(p => p.GenerateForm5110_28Async(reportData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult("/tmp/new.pdf", new byte[] { 1, 2, 3, 4 }));

        var controller = CreateController(25);
        var actionResult = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 8,
            Year = 2024
        });

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("Form_5110_28_08_2024.pdf", fileResult.FileDownloadName);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, fileResult.FileContents);

        var savedReport = await dbContext.TtbMonthlyReports.SingleAsync(r => r.CompanyId == 10);
        Assert.Equal(TtbReportStatus.Draft, savedReport.Status);
        Assert.Equal(10, savedReport.CompanyId);
        Assert.Equal(8, savedReport.ReportMonth);
        Assert.Equal(2024, savedReport.ReportYear);
        Assert.Equal(25, savedReport.CreatedByUserId);
    }

    [Fact]
    public async Task Generate_WhenReportHasNoContent_ReturnsBadRequest()
    {
        var emptyReport = new TtbMonthlyReportData
        {
            CompanyId = 10,
            Month = 7,
            Year = 2024,
            OpeningInventory = new InventorySection { Rows = Array.Empty<TtbSectionTotal>() },
            Production = new ProductionSection { Rows = Array.Empty<TtbSectionTotal>() },
            Transfers = new TransfersSection
            {
                TransfersIn = Array.Empty<TtbSectionTotal>(),
                TransfersOut = Array.Empty<TtbSectionTotal>()
            },
            Losses = new LossSection { Rows = Array.Empty<TtbSectionTotal>() },
            ClosingInventory = new InventorySection { Rows = Array.Empty<TtbSectionTotal>() }
        };

        calculator.Setup(c => c.CalculateMonthlyReportAsync(10, 7, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var controller = CreateController(25);
        var result = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 7,
            Year = 2024
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private TtbReportsController CreateController(int userId)
    {
        usersService.Setup(u => u.GetUserByIdAsync(userId)).ReturnsAsync(dbContext.Users.Find(userId));

        var controller = new TtbReportsController(
            dbContext,
            calculator.Object,
            pdfGenerator.Object,
            usersService.Object,
            NullLogger<TtbReportsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }
}
