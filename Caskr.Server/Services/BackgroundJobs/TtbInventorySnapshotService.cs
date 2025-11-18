using System;
using System.Collections.Generic;
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

public interface ITtbInventorySnapshotBackfillService
{
    Task BackfillSnapshotsAsync(int companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

public sealed class TtbInventorySnapshotService : IHostedService, IDisposable, ITtbInventorySnapshotBackfillService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TtbInventorySnapshotService> _logger;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public TtbInventorySnapshotService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TtbInventorySnapshotService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TTB inventory snapshot service.");
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

        _logger.LogInformation("Stopping TTB inventory snapshot service.");
        _stoppingCts?.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }

    public async Task BackfillSnapshotsAsync(int companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("The start date must be on or before the end date.", nameof(startDate));
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var calculator = scope.ServiceProvider.GetRequiredService<ITtbInventorySnapshotCalculator>();

        var current = startDate.Date;
        var last = endDate.Date;

        while (current <= last)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshots = await calculator.BuildSnapshotRowsAsync(companyId, current, cancellationToken);
            await ReplaceSnapshotsAsync(dbContext, companyId, current, snapshots, cancellationToken);
            _logger.LogInformation(
                "Backfilled {Count} TTB inventory snapshot rows for company {CompanyId} on {SnapshotDate:yyyy-MM-dd}.",
                snapshots.Count,
                companyId,
                current);
            current = current.AddDays(1);
        }
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextMidnight(DateTime.UtcNow);

            if (delay > TimeSpan.Zero)
            {
                _logger.LogDebug("TTB inventory snapshot service sleeping for {Delay}.", delay);
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
                await CaptureSnapshotsForAllCompaniesAsync(DateTime.UtcNow.Date, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while capturing TTB inventory snapshots.");
            }
        }
    }

    private async Task CaptureSnapshotsForAllCompaniesAsync(DateTime snapshotDate, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var calculator = scope.ServiceProvider.GetRequiredService<ITtbInventorySnapshotCalculator>();

        var companyIds = await GetTtbEnabledCompanyIdsAsync(dbContext, cancellationToken);
        if (companyIds.Count == 0)
        {
            _logger.LogDebug("No TTB-enabled companies were found for snapshot date {SnapshotDate:yyyy-MM-dd}.", snapshotDate);
            return;
        }

        foreach (var companyId in companyIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshots = await calculator.BuildSnapshotRowsAsync(companyId, snapshotDate, cancellationToken);
            await ReplaceSnapshotsAsync(dbContext, companyId, snapshotDate, snapshots, cancellationToken);

            _logger.LogInformation(
                "Captured {Count} TTB inventory snapshot rows for company {CompanyId} on {SnapshotDate:yyyy-MM-dd}.",
                snapshots.Count,
                companyId,
                snapshotDate);
        }
    }

    private static async Task ReplaceSnapshotsAsync(
        CaskrDbContext dbContext,
        int companyId,
        DateTime snapshotDate,
        IReadOnlyList<TtbInventorySnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var normalizedDate = snapshotDate.Date;
        var existing = await dbContext.TtbInventorySnapshots
            .Where(s => s.CompanyId == companyId && s.SnapshotDate == normalizedDate)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.TtbInventorySnapshots.RemoveRange(existing);
        }

        if (snapshots.Count > 0)
        {
            await dbContext.TtbInventorySnapshots.AddRangeAsync(snapshots, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyCollection<int>> GetTtbEnabledCompanyIdsAsync(
        CaskrDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var companyIds = new HashSet<int>();

        var permittedCompanies = await dbContext.Companies
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.TtbPermitNumber))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var id in permittedCompanies)
        {
            companyIds.Add(id);
        }

        var reportedCompanyIds = await dbContext.TtbMonthlyReports
            .AsNoTracking()
            .Select(r => r.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var id in reportedCompanyIds)
        {
            companyIds.Add(id);
        }

        var transactionCompanyIds = await dbContext.TtbTransactions
            .AsNoTracking()
            .Select(t => t.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var id in transactionCompanyIds)
        {
            companyIds.Add(id);
        }

        return companyIds;
    }

    private static TimeSpan CalculateDelayUntilNextMidnight(DateTime utcNow)
    {
        var nextMidnight = utcNow.Date.AddDays(1);
        return nextMidnight - utcNow;
    }
}
