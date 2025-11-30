using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly Mock<ITtbReportWorkflowService> workflowService = new();
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
            FormType = TtbFormType.Form5110_28,
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
    public async Task Generate_WhenYearBefore2020_ReturnsBadRequest()
    {
        var controller = CreateController(25);

        var actionResult = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 9,
            Year = 2019
        });

        var problem = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Contains("2020 or later", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
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
            FormType = TtbFormType.Form5110_28,
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
        Assert.Equal(TtbFormType.Form5110_28, savedReport.FormType);
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

    [Fact]
    public async Task Generate_WhenStorageReportRequested_UsesStorageServices()
    {
        var storageData = new TtbForm5110_40Data
        {
            CompanyId = 10,
            Month = 8,
            Year = 2024,
            OpeningBarrels = 5m,
            BarrelsReceived = 2m,
            BarrelsRemoved = 1m,
            ClosingBarrels = 6m,
            ProofGallonsByWarehouse = new[]
            {
                new WarehouseProofGallons { WarehouseName = "Main", ProofGallons = 120m }
            }
        };

        calculator.Setup(c => c.CalculateForm5110_40Async(10, 8, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageData);

        pdfGenerator.Setup(p => p.GenerateForm5110_40Async(storageData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult("/tmp/storage.pdf", new byte[] { 9, 8, 7 }));

        var controller = CreateController(25);
        var actionResult = await controller.Generate(new TtbReportGenerationRequest
        {
            CompanyId = 10,
            Month = 8,
            Year = 2024,
            FormType = TtbFormType.Form5110_40
        });

        calculator.Verify(c => c.CalculateMonthlyReportAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), default), Times.Never);
        calculator.Verify(c => c.CalculateForm5110_40Async(10, 8, 2024, It.IsAny<CancellationToken>()), Times.Once);

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("Form_5110_40_08_2024.pdf", fileResult.FileDownloadName);
        Assert.Equal(new byte[] { 9, 8, 7 }, fileResult.FileContents);

        var savedReport = await dbContext.TtbMonthlyReports.SingleAsync(r => r.CompanyId == 10 && r.FormType == TtbFormType.Form5110_40);
        Assert.Equal(TtbFormType.Form5110_40, savedReport.FormType);
    }

    [Fact]
    public async Task Generate_WhenRegenerationFails_KeepsExistingPdf()
    {
        var existingPdfPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(existingPdfPath, "existing draft pdf");

        try
        {
            dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
            {
                CompanyId = 10,
                ReportMonth = 10,
                ReportYear = 2024,
                Status = TtbReportStatus.Draft,
                GeneratedAt = DateTime.UtcNow.AddDays(-2),
                PdfPath = existingPdfPath,
                CreatedByUserId = 25
            });

            await dbContext.SaveChangesAsync();

            var emptyReport = new TtbMonthlyReportData
            {
                CompanyId = 10,
                Month = 10,
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

            calculator.Setup(c => c.CalculateMonthlyReportAsync(10, 10, 2024, It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyReport);

            var controller = CreateController(25);
            var result = await controller.Generate(new TtbReportGenerationRequest
            {
                CompanyId = 10,
                Month = 10,
                Year = 2024
            });

            dbContext.ChangeTracker.Clear();

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.True(File.Exists(existingPdfPath));

            var savedReport = await dbContext.TtbMonthlyReports.SingleAsync(r => r.CompanyId == 10 && r.ReportMonth == 10 && r.ReportYear == 2024);
            Assert.Equal(existingPdfPath, savedReport.PdfPath);
        }
        finally
        {
            if (File.Exists(existingPdfPath))
            {
                File.Delete(existingPdfPath);
            }
        }
    }

    [Fact]
    public async Task List_WithStatusFilter_ReturnsCompanyReportsForYear()
    {
        dbContext.TtbMonthlyReports.AddRange(
            new TtbMonthlyReport
            {
                CompanyId = 10,
                ReportMonth = 8,
                ReportYear = 2024,
                Status = TtbReportStatus.Draft,
                GeneratedAt = DateTime.UtcNow.AddDays(-2),
                CreatedByUserId = 25
            },
            new TtbMonthlyReport
            {
            CompanyId = 10,
            ReportMonth = 7,
            ReportYear = 2024,
            Status = TtbReportStatus.Submitted,
            FormType = TtbFormType.Form5110_28,
            GeneratedAt = DateTime.UtcNow.AddDays(-10),
            CreatedByUserId = 25
        },
            new TtbMonthlyReport
        {
            CompanyId = 10,
            ReportMonth = 6,
            ReportYear = 2023,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            GeneratedAt = DateTime.UtcNow.AddDays(-40),
            CreatedByUserId = 25
        },
            new TtbMonthlyReport
        {
            CompanyId = 22,
            ReportMonth = 8,
            ReportYear = 2024,
            Status = TtbReportStatus.Submitted,
            FormType = TtbFormType.Form5110_28,
            GeneratedAt = DateTime.UtcNow.AddDays(-3),
            CreatedByUserId = 30
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        var actionResult = await controller.List(10, 2024, status: TtbReportStatus.Submitted);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsAssignableFrom<IEnumerable<TtbReportSummaryResponse>>(ok.Value);
        var reports = response.ToList();

        var submittedReport = Assert.Single(reports);
        Assert.Equal(7, submittedReport.ReportMonth);
        Assert.Equal(TtbReportStatus.Submitted, submittedReport.Status);
        Assert.Equal(10, submittedReport.CompanyId);
        Assert.Equal(2024, submittedReport.ReportYear);
        Assert.Equal(TtbFormType.Form5110_28, submittedReport.FormType);
    }

    [Fact]
    public async Task List_WithFormTypeFilter_ReturnsOnlyMatchingForms()
    {
        dbContext.TtbMonthlyReports.AddRange(
            new TtbMonthlyReport
            {
                CompanyId = 10,
                ReportMonth = 8,
                ReportYear = 2024,
                Status = TtbReportStatus.Draft,
                FormType = TtbFormType.Form5110_40,
                GeneratedAt = DateTime.UtcNow.AddDays(-2),
                CreatedByUserId = 25
            },
            new TtbMonthlyReport
            {
                CompanyId = 10,
                ReportMonth = 7,
                ReportYear = 2024,
                Status = TtbReportStatus.Submitted,
                FormType = TtbFormType.Form5110_28,
                GeneratedAt = DateTime.UtcNow.AddDays(-10),
                CreatedByUserId = 25
            },
            new TtbMonthlyReport
            {
                CompanyId = 10,
                ReportMonth = 6,
                ReportYear = 2024,
                Status = TtbReportStatus.Draft,
                FormType = TtbFormType.Form5110_28,
                GeneratedAt = DateTime.UtcNow.AddDays(-20),
                CreatedByUserId = 25
            },
            new TtbMonthlyReport
            {
                CompanyId = 22,
                ReportMonth = 8,
                ReportYear = 2024,
                Status = TtbReportStatus.Submitted,
                FormType = TtbFormType.Form5110_40,
                GeneratedAt = DateTime.UtcNow.AddDays(-3),
                CreatedByUserId = 30
            });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        var actionResult = await controller.List(10, 2024, formType: TtbFormType.Form5110_40);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsAssignableFrom<IEnumerable<TtbReportSummaryResponse>>(ok.Value);
        var reports = response.ToList();

        var storageReport = Assert.Single(reports);
        Assert.Equal(8, storageReport.ReportMonth);
        Assert.Equal(TtbFormType.Form5110_40, storageReport.FormType);
        Assert.Equal(TtbReportStatus.Draft, storageReport.Status);
    }

    [Fact]
    public async Task Download_WhenPdfExists_ReturnsFileContents()
    {
        var pdfPath = Path.GetTempFileName();
        var pdfBytes = new byte[] { 5, 4, 3, 2 };
        await File.WriteAllBytesAsync(pdfPath, pdfBytes);

        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 10,
            ReportMonth = 9,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            GeneratedAt = DateTime.UtcNow,
            PdfPath = pdfPath,
            CreatedByUserId = 25
        });

        await dbContext.SaveChangesAsync();

        try
        {
            var controller = CreateController(25);
            var actionResult = await controller.Download(dbContext.TtbMonthlyReports.Single().Id);

            var fileResult = Assert.IsType<FileContentResult>(actionResult);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("Form_5110_28_09_2024.pdf", fileResult.FileDownloadName);
            Assert.Equal(pdfBytes, fileResult.FileContents);
        }
        finally
        {
            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
            }
        }
    }

    private TtbReportsController CreateController(int userId)
    {
        usersService.Setup(u => u.GetUserByIdAsync(userId)).ReturnsAsync(dbContext.Users.Find(userId));

        var controller = new TtbReportsController(
            dbContext,
            calculator.Object,
            pdfGenerator.Object,
            workflowService.Object,
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
