using System;
using System.IO;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Caskr.Server.Tests;

public class LabelsServiceTests
{
    private static LabelsService CreateService(CaskrDbContext dbContext, string contentRootPath)
    {
        var env = new TestWebHostEnvironment(contentRootPath);
        return new LabelsService(dbContext, env, NullLogger<LabelsService>.Instance);
    }

    private static CaskrDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CaskrDbContext(options);
        context.Companies.Add(new Company
        {
            Id = 7,
            CompanyName = "Sample Spirits",
            AddressLine1 = "123 Main St",
            City = "Portland",
            State = "OR",
            PostalCode = "97201",
            Country = "USA",
            TtbPermitNumber = "OR-12345",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GenerateTtbFormAsync_UsesEmbeddedTemplate()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var pdf = await service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 7,
            BrandName = "Caskr Reserve",
            ProductName = "Straight Bourbon",
            AlcoholContent = "45%"
        });

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        Assert.Equal((byte)'%', pdf[0]);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WhenTemplateMissing_ThrowsFileNotFound()
    {
        using var context = CreateContext();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var service = CreateService(context, tempRoot);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 7,
            BrandName = "No Template",
            ProductName = "No Template",
            AlcoholContent = "40%"
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
    public async Task GenerateTtbFormAsync_WithMissingBrand_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 7,
            ProductName = "Widget",
            AlcoholContent = "5%"
        }));

        Assert.Contains("Brand name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WithMissingProduct_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 7,
            BrandName = "Widget Brand",
            AlcoholContent = "12%"
        }));

        Assert.Contains("Product name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateTtbFormAsync_WhenCompanyMissing_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var solutionRoot = TestEnvironmentHelper.FindSolutionRoot();
        var service = CreateService(context, Path.Combine(solutionRoot, "Caskr.Server"));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 404,
            BrandName = "Ghost Co.",
            ProductName = "Phantom",
            AlcoholContent = "0%"
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

        var templatePath = Path.Combine(formsDirectory, "ttb_form_5100_31.pdf");
        using (var writer = new iText.Kernel.Pdf.PdfWriter(templatePath))
        using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
        using (var document = new iText.Layout.Document(pdf))
        {
            document.Add(new iText.Layout.Element.Paragraph("Placeholder template without form fields"));
        }

        var service = CreateService(context, tempRoot);

        var pdfBytes = await service.GenerateTtbFormAsync(new LabelRequest
        {
            CompanyId = 7,
            BrandName = "Template Lite",
            ProductName = "Formless",
            AlcoholContent = "10%"
        });

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
    }
}
