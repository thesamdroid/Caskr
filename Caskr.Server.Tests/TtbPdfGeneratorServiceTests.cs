using System.Globalization;
using System.IO;
using Caskr.server.Models;
using Caskr.server.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Caskr.Server.Tests;

public class TtbPdfGeneratorServiceTests
{
    [Fact]
    public async Task GenerateForm5110_28Async_FillsAndSavesPdf()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CaskrDbContext(options);
        var company = new Company
        {
            Id = 15,
            CompanyName = "ACME Distilling",
            AddressLine1 = "123 Oak Street",
            City = "Austin",
            State = "TX",
            PostalCode = "78701",
            Country = "USA",
            TtbPermitNumber = "TX-12345",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var reportData = new TtbMonthlyReportData
        {
            CompanyId = company.Id,
            Month = 2,
            Year = 2024,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 29),
            OpeningInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 10.5m,
                        WineGallons = 5.25m
                    }
                }
            },
            Production = new ProductionSection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 5.2m,
                        WineGallons = 2.6m
                    }
                }
            },
            Transfers = new TransfersSection
            {
                TransfersIn = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Neutral Spirits",
                        SpiritsType = TtbSpiritsType.Neutral190OrMore,
                        ProofGallons = 3.1m,
                        WineGallons = 1.55m
                    }
                },
                TransfersOut = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 1.75m,
                        WineGallons = 0.9m
                    }
                }
            },
            Losses = new LossSection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 0.25m,
                        WineGallons = 0.1m
                    }
                }
            },
            ClosingInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Whiskey",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 14m,
                        WineGallons = 6.9m
                    },
                    new TtbSectionTotal
                    {
                        ProductType = "Neutral Spirits",
                        SpiritsType = TtbSpiritsType.Neutral190OrMore,
                        ProofGallons = 2.5m,
                        WineGallons = 1.2m
                    }
                }
            }
        };

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var formsDirectory = Path.Combine(tempRoot, "Forms");
        Directory.CreateDirectory(formsDirectory);

        var projectRoot = TestEnvironmentHelper.FindSolutionRoot();
        var sourceTemplate = Path.Combine(projectRoot, "Caskr.Server", "Forms", "ttb_form_5110_28.pdf");
        var testTemplate = Path.Combine(formsDirectory, "ttb_form_5110_28.pdf");
        File.Copy(sourceTemplate, testTemplate, true);

        var env = new TestWebHostEnvironment(tempRoot);
        var service = new TtbPdfGeneratorService(context, env, NullLogger<TtbPdfGeneratorService>.Instance);

        var result = await service.GenerateForm5110_28Async(reportData);

        var expectedPath = Path.Combine(
            tempRoot,
            "Storage",
            "TTBReports",
            company.Id.ToString(CultureInfo.InvariantCulture),
            reportData.Year.ToString("D4", CultureInfo.InvariantCulture),
            reportData.Month.ToString("D2", CultureInfo.InvariantCulture),
            "Form_5110_28.pdf");

        Assert.Equal(expectedPath, result.FilePath);
        Assert.True(File.Exists(result.FilePath));
        Assert.NotEmpty(result.Content);

        using var reader = new PdfReader(new MemoryStream(result.Content));
        using var pdfDocument = new PdfDocument(reader);
        var firstPageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(1));

        Assert.Contains("ACME Distilling", firstPageText);
        Assert.Contains("TX-12345", firstPageText);
        Assert.Contains("02", firstPageText);
        Assert.Contains("2024", firstPageText);
        Assert.Contains("10.50", firstPageText);
        Assert.Contains("3.10", firstPageText);
        Assert.Contains("14.00", firstPageText);
        Assert.Contains("2.50", firstPageText);
    }

    [Fact]
    public async Task GenerateForm5110_40Async_FillsAndSavesPdf()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CaskrDbContext(options);
        var company = new Company
        {
            Id = 16,
            CompanyName = "Storage Spirits Co.",
            AddressLine1 = "500 Barrel Row",
            City = "Frankfort",
            State = "KY",
            PostalCode = "40601",
            Country = "USA",
            TtbPermitNumber = "KY-54321",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var reportData = new TtbForm5110_40Data
        {
            CompanyId = company.Id,
            Month = 3,
            Year = 2024,
            OpeningBarrels = 10m,
            BarrelsReceived = 2m,
            BarrelsRemoved = 1m,
            ClosingBarrels = 11m,
            ProofGallonsByWarehouse = new[]
            {
                new WarehouseProofGallons { WarehouseName = "Main Warehouse", ProofGallons = 500m }
            }
        };

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var formsDirectory = Path.Combine(tempRoot, "Forms");
        Directory.CreateDirectory(formsDirectory);

        var projectRoot = TestEnvironmentHelper.FindSolutionRoot();
        File.Copy(Path.Combine(projectRoot, "Caskr.Server", "Forms", "ttb_form_5110_40.pdf"),
            Path.Combine(formsDirectory, "ttb_form_5110_40.pdf"), true);

        var env = new TestWebHostEnvironment(tempRoot);
        var service = new TtbPdfGeneratorService(context, env, NullLogger<TtbPdfGeneratorService>.Instance);

        var result = await service.GenerateForm5110_40Async(reportData);

        var expectedPath = Path.Combine(
            tempRoot,
            "Storage",
            "TTBReports",
            company.Id.ToString(CultureInfo.InvariantCulture),
            reportData.Year.ToString("D4", CultureInfo.InvariantCulture),
            reportData.Month.ToString("D2", CultureInfo.InvariantCulture),
            "Form_5110_40.pdf");

        Assert.Equal(expectedPath, result.FilePath);
        Assert.True(File.Exists(result.FilePath));
        Assert.NotEmpty(result.Content);

        using var reader = new PdfReader(new MemoryStream(result.Content));
        using var pdfDocument = new PdfDocument(reader);
        var firstPageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(1));

        Assert.Contains("Storage Spirits Co.", firstPageText);
        Assert.Contains("KY-54321", firstPageText);
        Assert.Contains("03", firstPageText);
        Assert.Contains("2024", firstPageText);
    }

    [Fact]
    public async Task GenerateForm5110_40Async_WhenTemplateMissing_ThrowsFileNotFound()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CaskrDbContext(options);
        context.Companies.Add(new Company
        {
            Id = 21,
            CompanyName = "Missing Template Spirits",
            AddressLine1 = "1 Compliance Way",
            City = "Louisville",
            State = "KY",
            PostalCode = "40202",
            Country = "USA",
            TtbPermitNumber = "KY-00000",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });

        await context.SaveChangesAsync();

        var reportData = new TtbForm5110_40Data
        {
            CompanyId = 21,
            Month = 1,
            Year = 2025,
            OpeningBarrels = 1m,
            BarrelsReceived = 0m,
            BarrelsRemoved = 0m,
            ClosingBarrels = 1m,
            ProofGallonsByWarehouse = Array.Empty<WarehouseProofGallons>()
        };

        var env = new TestWebHostEnvironment(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Directory.CreateDirectory(env.ContentRootPath);

        var service = new TtbPdfGeneratorService(context, env, NullLogger<TtbPdfGeneratorService>.Instance);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GenerateForm5110_40Async(reportData));
    }

    /// <summary>
    /// CI smoke test: Verifies that TTB form templates exist in the expected location.
    /// This test ensures document generation will not fail due to missing templates.
    /// </summary>
    [Fact]
    public void FormTemplates_ExistInExpectedLocation()
    {
        var projectRoot = TestEnvironmentHelper.FindSolutionRoot();
        var formsDirectory = Path.Combine(projectRoot, "Caskr.Server", "Forms");

        Assert.True(Directory.Exists(formsDirectory), $"Forms directory not found at {formsDirectory}");

        var form5110_28Path = Path.Combine(formsDirectory, "ttb_form_5110_28.pdf");
        var form5110_40Path = Path.Combine(formsDirectory, "ttb_form_5110_40.pdf");

        Assert.True(File.Exists(form5110_28Path), $"TTB Form 5110.28 template not found at {form5110_28Path}");
        Assert.True(File.Exists(form5110_40Path), $"TTB Form 5110.40 template not found at {form5110_40Path}");

        // Verify templates are valid PDFs with non-zero size
        var form5110_28Info = new FileInfo(form5110_28Path);
        var form5110_40Info = new FileInfo(form5110_40Path);

        Assert.True(form5110_28Info.Length > 0, "TTB Form 5110.28 template is empty");
        Assert.True(form5110_40Info.Length > 0, "TTB Form 5110.40 template is empty");
    }

    /// <summary>
    /// CI smoke test: Verifies that PDF generation produces a valid, non-empty document.
    /// This test catches issues with the PDF generation pipeline early in CI.
    /// </summary>
    [Fact]
    public async Task GenerateForm5110_28Async_ProducesValidPdf_SmokeTest()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CaskrDbContext(options);
        var company = new Company
        {
            Id = 100,
            CompanyName = "CI Test Distillery",
            AddressLine1 = "123 Test Lane",
            City = "Test City",
            State = "TX",
            PostalCode = "12345",
            Country = "USA",
            TtbPermitNumber = "CI-TEST-001",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Minimal report data for smoke test
        var reportData = new TtbMonthlyReportData
        {
            CompanyId = company.Id,
            Month = 1,
            Year = 2025,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            OpeningInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Test Spirit",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 100m,
                        WineGallons = 50m
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
                        ProductType = "Test Spirit",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 100m,
                        WineGallons = 50m
                    }
                }
            }
        };

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var formsDirectory = Path.Combine(tempRoot, "Forms");
        Directory.CreateDirectory(formsDirectory);

        var projectRoot = TestEnvironmentHelper.FindSolutionRoot();
        var sourceTemplate = Path.Combine(projectRoot, "Caskr.Server", "Forms", "ttb_form_5110_28.pdf");
        var testTemplate = Path.Combine(formsDirectory, "ttb_form_5110_28.pdf");
        File.Copy(sourceTemplate, testTemplate, true);

        var env = new TestWebHostEnvironment(tempRoot);
        var service = new TtbPdfGeneratorService(context, env, NullLogger<TtbPdfGeneratorService>.Instance);

        // Act
        var result = await service.GenerateForm5110_28Async(reportData);

        // Assert - PDF was generated
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(File.Exists(result.FilePath), $"Generated PDF file not found at {result.FilePath}");

        // Assert - PDF is valid and readable
        using var reader = new PdfReader(new MemoryStream(result.Content));
        using var pdfDocument = new PdfDocument(reader);
        Assert.True(pdfDocument.GetNumberOfPages() > 0, "Generated PDF has no pages");

        // Assert - PDF contains expected content
        var firstPageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(1));
        Assert.Contains("CI Test Distillery", firstPageText);
        Assert.Contains("CI-TEST-001", firstPageText);

        // Cleanup
        try
        {
            Directory.Delete(tempRoot, true);
        }
        catch
        {
            // Ignore cleanup errors in test
        }
    }

    /// <summary>
    /// CI smoke test: Verifies that Form 5110.40 PDF generation produces a valid document.
    /// </summary>
    [Fact]
    public async Task GenerateForm5110_40Async_ProducesValidPdf_SmokeTest()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CaskrDbContext(options);
        var company = new Company
        {
            Id = 101,
            CompanyName = "CI Test Storage Co.",
            AddressLine1 = "456 Storage Road",
            City = "Warehouse City",
            State = "KY",
            PostalCode = "40601",
            Country = "USA",
            TtbPermitNumber = "CI-STORAGE-001",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var reportData = new TtbForm5110_40Data
        {
            CompanyId = company.Id,
            Month = 1,
            Year = 2025,
            OpeningBarrels = 10m,
            BarrelsReceived = 2m,
            BarrelsRemoved = 1m,
            ClosingBarrels = 11m,
            ProofGallonsByWarehouse = new[]
            {
                new WarehouseProofGallons { WarehouseName = "Test Warehouse", ProofGallons = 500m }
            }
        };

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var formsDirectory = Path.Combine(tempRoot, "Forms");
        Directory.CreateDirectory(formsDirectory);

        var projectRoot = TestEnvironmentHelper.FindSolutionRoot();
        File.Copy(
            Path.Combine(projectRoot, "Caskr.Server", "Forms", "ttb_form_5110_40.pdf"),
            Path.Combine(formsDirectory, "ttb_form_5110_40.pdf"),
            true);

        var env = new TestWebHostEnvironment(tempRoot);
        var service = new TtbPdfGeneratorService(context, env, NullLogger<TtbPdfGeneratorService>.Instance);

        // Act
        var result = await service.GenerateForm5110_40Async(reportData);

        // Assert - PDF was generated
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.True(File.Exists(result.FilePath), $"Generated PDF file not found at {result.FilePath}");

        // Assert - PDF is valid and readable
        using var reader = new PdfReader(new MemoryStream(result.Content));
        using var pdfDocument = new PdfDocument(reader);
        Assert.True(pdfDocument.GetNumberOfPages() > 0, "Generated PDF has no pages");

        // Assert - PDF contains expected content
        var firstPageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(1));
        Assert.Contains("CI Test Storage Co.", firstPageText);
        Assert.Contains("CI-STORAGE-001", firstPageText);

        // Cleanup
        try
        {
            Directory.Delete(tempRoot, true);
        }
        catch
        {
            // Ignore cleanup errors in test
        }
    }
}
