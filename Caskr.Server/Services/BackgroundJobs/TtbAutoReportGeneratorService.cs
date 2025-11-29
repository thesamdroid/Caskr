using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services.BackgroundJobs;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public interface ITtbAutoReportProcessor
{
    Task<DateTimeOffset> CalculateNextRunAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);

    Task GenerateReportsAsync(DateTimeOffset runTime, CancellationToken cancellationToken);
}

/// <summary>
/// Generates TTB Form 5110.28 monthly draft reports on a scheduled cadence.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md
/// REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V - Records and Reports
///
/// This hosted service schedules background report generation according to
/// company auto-generation preferences. It calculates the previous month's
/// reporting period, invokes the TTB calculator to ensure official formulas are
/// used, produces draft PDFs, and notifies compliance contacts.
///
/// CRITICAL: This service generates data for federal compliance reporting.
/// Any modification must be reviewed against TTB regulations and the mapping document.
/// </summary>
public sealed class TtbAutoReportGeneratorService : IHostedService, IDisposable
{
    private readonly ITtbAutoReportProcessor _processor;
    private readonly ISystemClock _clock;
    private readonly ILogger<TtbAutoReportGeneratorService> _logger;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public TtbAutoReportGeneratorService(
        ITtbAutoReportProcessor processor,
        ISystemClock clock,
        ILogger<TtbAutoReportGeneratorService> logger)
    {
        _processor = processor;
        _clock = clock;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TTB auto-report generator service.");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask is null)
        {
            return;
        }

        _logger.LogInformation("Stopping TTB auto-report generator service.");
        _stoppingCts?.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            DateTimeOffset nextRun;
            try
            {
                nextRun = await _processor.CalculateNextRunAsync(_clock.UtcNow, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate next TTB auto-report run; retrying in 24 hours.");
                nextRun = _clock.UtcNow.AddHours(24);
            }

            var delay = nextRun - _clock.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                _logger.LogDebug("TTB auto-report generator sleeping for {Delay} until {NextRunUtc}.", delay, nextRun);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                await _processor.GenerateReportsAsync(nextRun, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while generating scheduled TTB reports.");
            }
        }
    }
}

public sealed class TtbAutoReportProcessor : ITtbAutoReportProcessor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TtbAutoReportProcessor> _logger;

    public TtbAutoReportProcessor(IServiceScopeFactory serviceScopeFactory, ILogger<TtbAutoReportProcessor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task<DateTimeOffset> CalculateNextRunAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();

        var companies = await dbContext.Companies
            .AsNoTracking()
            .Where(c => c.AutoGenerateTtbReports)
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
        {
            return CalculateNextMonthlyRun(referenceTime, 1, 6);
        }

        return companies
            .Select(company => CalculateCompanyNextRun(company, referenceTime))
            .Min();
    }

    public async Task GenerateReportsAsync(DateTimeOffset runTime, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var calculator = scope.ServiceProvider.GetRequiredService<ITtbReportCalculator>();
        var pdfGenerator = scope.ServiceProvider.GetRequiredService<ITtbPdfGenerator>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var companies = await dbContext.Companies
            .AsNoTracking()
            .Include(c => c.Users)
            .ThenInclude(u => u.UserType)
            .Where(c => c.AutoGenerateTtbReports)
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
        {
            _logger.LogInformation("No companies configured for TTB auto-generation at {RunTimeUtc}.", runTime);
            return;
        }

        var (reportMonth, reportYear) = GetReportingPeriod(runTime);
        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scheduledTime = CalculateCompanyNextRun(company, runTime.AddSeconds(-1));
            if (scheduledTime > runTime)
            {
                _logger.LogDebug(
                    "Skipping company {CompanyId} because its next scheduled run ({Scheduled}) is after {RunTimeUtc}.",
                    company.Id,
                    scheduledTime,
                    runTime);
                continue;
            }

            var existingReport = await dbContext.TtbMonthlyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.CompanyId == company.Id
                    && r.ReportMonth == reportMonth
                    && r.ReportYear == reportYear
                    && r.FormType == TtbFormType.Form5110_28,
                    cancellationToken);

            if (existingReport != null)
            {
                _logger.LogInformation(
                    "TTB report for company {CompanyId} {Month}/{Year} already exists (Id={ReportId}); skipping auto-generation.",
                    company.Id,
                    reportMonth,
                    reportYear,
                    existingReport.Id);
                continue;
            }

            TtbMonthlyReportData reportData;
            try
            {
                reportData = await calculator.CalculateMonthlyReportAsync(company.Id, reportMonth, reportYear, cancellationToken);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                _logger.LogError(
                    ex,
                    "TTB COMPLIANCE ERROR: Calculation failed for company {CompanyId} month {Month} year {Year}. See docs/TTB_FORM_5110_28_MAPPING.md for calculation rules.",
                    company.Id,
                    reportMonth,
                    reportYear);
                continue;
            }

            if (!HasReportContent(reportData))
            {
                _logger.LogWarning(
                    "TTB auto-generation skipped for company {CompanyId} {Month}/{Year} because no reportable activity was found.",
                    company.Id,
                    reportMonth,
                    reportYear);
                continue;
            }

            PdfGenerationResult pdfResult;
            try
            {
                pdfResult = await pdfGenerator.GenerateForm5110_28Async(reportData, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "TTB COMPLIANCE ERROR: PDF generation failed for company {CompanyId} month {Month} year {Year}.",
                    company.Id,
                    reportMonth,
                    reportYear);
                continue;
            }

            if (pdfResult.Content.Length == 0)
            {
                _logger.LogError(
                    "Generated PDF was empty for company {CompanyId} month {Month} year {Year}. See docs/TTB_FORM_5110_28_MAPPING.md for form structure.",
                    company.Id,
                    reportMonth,
                    reportYear);
                continue;
            }

            var createdByUserId = ResolveComplianceUserId(company);
            if (createdByUserId == 0)
            {
                _logger.LogWarning(
                    "Unable to find compliance contact for company {CompanyId}; skipping TTB auto-generation for {Month}/{Year}.",
                    company.Id,
                    reportMonth,
                    reportYear);
                continue;
            }

            var report = new TtbMonthlyReport
            {
                CompanyId = company.Id,
                ReportMonth = reportMonth,
                ReportYear = reportYear,
                Status = TtbReportStatus.Draft,
                FormType = TtbFormType.Form5110_28,
                GeneratedAt = runTime.UtcDateTime,
                PdfPath = pdfResult.FilePath,
                CreatedByUserId = createdByUserId
            };

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.TtbMonthlyReports.Add(report);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await NotifyComplianceAsync(emailService, company, report, runTime);
            _logger.LogInformation(
                "Generated draft TTB report {ReportId} for company {CompanyId} covering {Month}/{Year} at {RunTimeUtc}.",
                report.Id,
                company.Id,
                reportMonth,
                reportYear,
                runTime);
        }
    }

    private static bool HasReportContent(TtbMonthlyReportData reportData)
    {
        return reportData.OpeningInventory.Rows.Any()
               || reportData.Production.Rows.Any()
               || reportData.Transfers.TransfersIn.Any()
               || reportData.Transfers.TransfersOut.Any()
               || reportData.Losses.Rows.Any()
               || reportData.ClosingInventory.Rows.Any();
    }

    private static (int Month, int Year) GetReportingPeriod(DateTimeOffset runTime)
    {
        // TTB 5110.28 requires reporting on the prior calendar month.
        // See docs/TTB_FORM_5110_28_MAPPING.md, section "Form Structure".
        var period = new DateTime(runTime.Year, runTime.Month, 1).AddMonths(-1);
        return (period.Month, period.Year);
    }

    private static DateTimeOffset CalculateCompanyNextRun(Company company, DateTimeOffset referenceTime)
    {
        var hour = Math.Clamp(company.TtbAutoReportHourUtc, 0, 23);

        return company.TtbAutoReportCadence == TtbAutoReportCadence.Weekly
            ? CalculateNextWeeklyRun(referenceTime, company.TtbAutoReportDayOfWeek, hour)
            : CalculateNextMonthlyRun(referenceTime, company.TtbAutoReportDayOfMonth, hour);
    }

    private static DateTimeOffset CalculateNextMonthlyRun(DateTimeOffset referenceTime, int dayOfMonth, int hourUtc)
    {
        var normalizedDay = Math.Clamp(dayOfMonth, 1, 28);
        var currentMonthDay = Math.Min(normalizedDay, DateTime.DaysInMonth(referenceTime.Year, referenceTime.Month));
        var candidate = new DateTimeOffset(
            referenceTime.Year,
            referenceTime.Month,
            currentMonthDay,
            hourUtc,
            0,
            0,
            TimeSpan.Zero);

        if (candidate <= referenceTime)
        {
            var nextMonth = new DateTime(referenceTime.Year, referenceTime.Month, 1).AddMonths(1);
            var nextDay = Math.Min(normalizedDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
            candidate = new DateTimeOffset(nextMonth.Year, nextMonth.Month, nextDay, hourUtc, 0, 0, TimeSpan.Zero);
        }

        return candidate;
    }

    private static DateTimeOffset CalculateNextWeeklyRun(DateTimeOffset referenceTime, DayOfWeek dayOfWeek, int hourUtc)
    {
        var daysUntil = ((int)dayOfWeek - (int)referenceTime.DayOfWeek + 7) % 7;
        var nextDate = referenceTime.Date.AddDays(daysUntil);
        var candidate = new DateTimeOffset(nextDate.Year, nextDate.Month, nextDate.Day, hourUtc, 0, 0, TimeSpan.Zero);

        if (candidate <= referenceTime)
        {
            candidate = candidate.AddDays(7);
        }

        return candidate;
    }

    private static int ResolveComplianceUserId(Company company)
    {
        var complianceUser = company.Users
            .FirstOrDefault(u => u.IsTtbContact || string.Equals(u.UserType?.Name, "Compliance", StringComparison.OrdinalIgnoreCase));

        if (complianceUser != null)
        {
            return complianceUser.Id;
        }

        var primaryContact = company.Users.FirstOrDefault(u => u.IsPrimaryContact);
        return primaryContact?.Id ?? company.Users.FirstOrDefault()?.Id ?? 0;
    }

    private static async Task NotifyComplianceAsync(IEmailService emailService, Company company, TtbMonthlyReport report, DateTimeOffset runTime)
    {
        var recipients = company.Users
            .Where(u => u.IsTtbContact || string.Equals(u.UserType?.Name, "Compliance", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Email)
            .Distinct()
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        var subject = "TTB Monthly Report Generated - Review Required";
        var link = $"/ttb-reports/{report.Id}";
        var body =
            $"A draft TTB Form 5110.28 for {report.ReportMonth:D2}/{report.ReportYear} was generated automatically at {runTime:O}. " +
            "Please review the report for compliance accuracy before submission. " +
            $"Access the draft here: {link}";

        foreach (var recipient in recipients)
        {
            await emailService.SendEmailAsync(recipient, subject, body);
        }
    }
}
