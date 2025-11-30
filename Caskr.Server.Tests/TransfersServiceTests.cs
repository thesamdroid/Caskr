using System;
using System.IO;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Caskr.Server.Tests;

public class TransfersServiceTests
{
    private static TransfersService CreateService(CaskrDbContext dbContext, string contentRootPath)
    {
        var env = new TestWebHostEnvironment(contentRootPath);
        return new TransfersService(dbContext, env, NullLogger<TransfersService>.Instance);
    }

    private static CaskrDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CaskrDbContext(options);
        context.Companies.Add(new Company
        {
            Id = 9,
            CompanyName = "Warehouse Transfers Inc.",
            AddressLine1 = "456 Warehouse Rd",
            City = "Louisville",
            State = "KY",
            PostalCode = "40202",
            Country = "USA",
            TtbPermitNumber = "KY-67890",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithValidRequest_ReturnsPdfBytes()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var pdf = await service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 9,
            ToCompanyName = "Bottling Co.",
            BarrelCount = 3,
            Address = "789 Bottling Ave",
            PermitNumber = "TX-22222",
            OrderId = null
        });

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        Assert.Equal('%', pdf[0]);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WhenTemplateMissing_ThrowsFileNotFound()
    {
        using var context = CreateContext();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var service = CreateService(context, tempRoot);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 9,
            ToCompanyName = "Missing Template",
            BarrelCount = 1
        }));
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithNullRequest_ThrowsArgumentNull()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GenerateTtbFormAsync(null!));
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithInvalidBarrelCount_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 9,
            ToCompanyName = "Bad Request Co.",
            BarrelCount = 0
        }));

        Assert.Contains("Barrel count", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithMissingDestinationName_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 9,
            BarrelCount = 2
        }));

        Assert.Contains("Destination company name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WhenCompanyMissing_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 100,
            ToCompanyName = "Missing Co.",
            BarrelCount = 1
        }));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithTemplateWithoutFormFields_CreatesSimplePdf()
    {
        using var context = CreateContext();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var formsDirectory = Path.Combine(tempRoot, "Forms");
        Directory.CreateDirectory(formsDirectory);

        var templatePath = Path.Combine(formsDirectory, "ttb_form_5100_16.pdf");
        using (var writer = new iText.Kernel.Pdf.PdfWriter(templatePath))
        using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
        using (var document = new iText.Layout.Document(pdf))
        {
            document.Add(new iText.Layout.Element.Paragraph("Placeholder transfer template without fields"));
        }

        var service = CreateService(context, tempRoot);

        var pdfBytes = await service.GenerateTtbFormAsync(new TransferRequest
        {
            FromCompanyId = 9,
            ToCompanyName = "Formless Destination",
            BarrelCount = 2,
            Address = "123 No Fields Ave"
        });

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
    }
}
