using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.Server.Services.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class TtbAutoReportGeneratorServiceTests
{
    [Fact]
    public async Task GenerateReportsAsync_CreatesDraftAndEmailsComplianceContacts()
    {
        var calculator = new Mock<ITtbReportCalculator>();
        calculator
            .Setup(service => service.CalculateMonthlyReportAsync(1, 12, 2023, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReportData(1, 12, 2023));

        var pdfGenerator = new Mock<ITtbPdfGenerator>();
        pdfGenerator
            .Setup(service => service.GenerateForm5110_28Async(It.IsAny<TtbMonthlyReportData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult("/reports/1.pdf", new byte[] { 1 }));

        var emailService = new Mock<IEmailService>();

        using var provider = BuildServiceProvider(calculator, pdfGenerator, emailService);
        await SeedCompanyAsync(provider, "Compliance", true);

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var processor = new TtbAutoReportProcessor(scopeFactory, Mock.Of<ILogger<TtbAutoReportProcessor>>());
        var runTime = new DateTimeOffset(2024, 1, 1, 6, 0, 0, TimeSpan.Zero);

        await processor.GenerateReportsAsync(runTime, CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var report = await dbContext.TtbMonthlyReports.SingleAsync();

        Assert.Equal(12, report.ReportMonth);
        Assert.Equal(2023, report.ReportYear);
        Assert.Equal(TtbReportStatus.Draft, report.Status);
        Assert.Equal(1, report.CreatedByUserId);
        Assert.Equal("/reports/1.pdf", report.PdfPath);

        emailService.Verify(service => service.SendEmailAsync(
            "compliance@example.com",
            "TTB Monthly Report Generated - Review Required",
            It.Is<string>(body => body.Contains("12/2023"))),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReportsAsync_SkipsWhenNoReportContent()
    {
        var calculator = new Mock<ITtbReportCalculator>();
        calculator
            .Setup(service => service.CalculateMonthlyReportAsync(1, 12, 2023, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TtbMonthlyReportData
            {
                CompanyId = 1,
                Month = 12,
                Year = 2023,
                StartDate = new DateTime(2023, 12, 1),
                EndDate = new DateTime(2023, 12, 31)
            });

        var pdfGenerator = new Mock<ITtbPdfGenerator>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);

        using var provider = BuildServiceProvider(calculator, pdfGenerator, emailService);
        await SeedCompanyAsync(provider, "Compliance", true);

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var processor = new TtbAutoReportProcessor(scopeFactory, Mock.Of<ILogger<TtbAutoReportProcessor>>());
        var runTime = new DateTimeOffset(2024, 1, 1, 6, 0, 0, TimeSpan.Zero);

        await processor.GenerateReportsAsync(runTime, CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        Assert.Empty(await dbContext.TtbMonthlyReports.ToListAsync());

        pdfGenerator.Verify(
            service => service.GenerateForm5110_28Async(It.IsAny<TtbMonthlyReportData>(), It.IsAny<CancellationToken>()),
            Times.Never);
        emailService.Verify(
            service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculateNextRunAsync_HonorsEarliestCompanySchedule()
    {
        var calculator = new Mock<ITtbReportCalculator>();
        var pdfGenerator = new Mock<ITtbPdfGenerator>();
        var emailService = new Mock<IEmailService>();

        using var provider = BuildServiceProvider(calculator, pdfGenerator, emailService);
        await SeedCompanyAsync(provider, "Compliance", true, cadence: TtbAutoReportCadence.Monthly, dayOfMonth: 1, hourUtc: 6);
        await SeedCompanyAsync(provider, "Compliance", true, companyId: 2, cadence: TtbAutoReportCadence.Weekly, dayOfWeek: DayOfWeek.Wednesday, hourUtc: 6);

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var processor = new TtbAutoReportProcessor(scopeFactory, Mock.Of<ILogger<TtbAutoReportProcessor>>());
        var reference = new DateTimeOffset(2024, 1, 15, 5, 0, 0, TimeSpan.Zero); // Monday

        var nextRun = await processor.CalculateNextRunAsync(reference, CancellationToken.None);

        var expected = new DateTimeOffset(2024, 1, 17, 6, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, nextRun);
    }

    private static ServiceProvider BuildServiceProvider(
        Mock<ITtbReportCalculator> calculator,
        Mock<ITtbPdfGenerator> pdfGenerator,
        Mock<IEmailService> emailService)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<CaskrDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped(_ => calculator.Object);
        services.AddScoped(_ => pdfGenerator.Object);
        services.AddScoped(_ => emailService.Object);
        return services.BuildServiceProvider();
    }

    private static async Task SeedCompanyAsync(
        ServiceProvider provider,
        string userTypeName,
        bool autoGenerate,
        int companyId = 1,
        TtbAutoReportCadence cadence = TtbAutoReportCadence.Monthly,
        int dayOfMonth = 1,
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        int hourUtc = 6)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            CompanyName = $"Test Company {companyId}",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow,
            AutoGenerateTtbReports = autoGenerate,
            TtbAutoReportCadence = cadence,
            TtbAutoReportDayOfMonth = dayOfMonth,
            TtbAutoReportDayOfWeek = dayOfWeek,
            TtbAutoReportHourUtc = hourUtc
        });

        dbContext.UserTypes.Add(new UserType
        {
            Id = companyId,
            Name = userTypeName
        });

        dbContext.Users.Add(new User
        {
            Id = companyId,
            CompanyId = companyId,
            Name = "Compliance Officer",
            Email = "compliance@example.com",
            IsPrimaryContact = true,
            UserTypeId = companyId,
            IsTtbContact = true,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static TtbMonthlyReportData CreateReportData(int companyId, int month, int year)
    {
        return new TtbMonthlyReportData
        {
            CompanyId = companyId,
            Month = month,
            Year = year,
            StartDate = new DateTime(year, month, 1),
            EndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)),
            OpeningInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Bourbon",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 10,
                        WineGallons = 5
                    }
                }
            },
            ClosingInventory = new InventorySection
            {
                Rows = new[]
                {
                    new TtbSectionTotal
                    {
                        ProductType = "Bourbon",
                        SpiritsType = TtbSpiritsType.Under190Proof,
                        ProofGallons = 8,
                        WineGallons = 4
                    }
                }
            }
        };
    }
}
