using System.Globalization;
using Caskr.Server.Services;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services.BackgroundJobs;

/// <summary>
///     Background worker that periodically syncs pending QuickBooks artifacts based on company preferences.
/// </summary>
public sealed class QuickBooksSyncHostedService : IHostedService, IDisposable
{
    private const string InvoiceEntityType = "Invoice";
    private const string BatchEntityType = "Batch";
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan HourlyInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan DailyInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<QuickBooksSyncHostedService> _logger;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public QuickBooksSyncHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<QuickBooksSyncHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting QuickBooks sync hosted service.");
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

        _logger.LogInformation("Stopping QuickBooks sync hosted service.");
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
        var nextDelay = DefaultPollingInterval;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                nextDelay = await ProcessCompaniesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while running the QuickBooks sync hosted service loop.");
                nextDelay = DefaultPollingInterval;
            }

            try
            {
                await Task.Delay(nextDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    ///     Processes all QuickBooks-enabled companies once and returns the suggested delay before the next iteration.
    /// </summary>
    internal async Task<TimeSpan> ProcessCompaniesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var invoiceSyncService = scope.ServiceProvider.GetRequiredService<IQuickBooksInvoiceSyncService>();
        var costTrackingService = scope.ServiceProvider.GetRequiredService<IQuickBooksCostTrackingService>();

        var connectedCompanyIds = await dbContext.AccountingIntegrations
            .AsNoTracking()
            .Where(ai => ai.Provider == AccountingProvider.QuickBooks && ai.IsActive)
            .Select(ai => ai.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (connectedCompanyIds.Count == 0)
        {
            _logger.LogDebug("QuickBooks sync hosted service found no connected companies.");
            return DefaultPollingInterval;
        }

        var preferences = await dbContext.AccountingSyncPreferences
            .Where(p => p.Provider == AccountingProvider.QuickBooks && connectedCompanyIds.Contains(p.CompanyId))
            .ToListAsync(cancellationToken);

        if (preferences.Count == 0)
        {
            _logger.LogDebug("QuickBooks sync hosted service found no sync preferences.");
            return DefaultPollingInterval;
        }

        foreach (var preference in preferences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ShouldProcessPreference(preference))
            {
                continue;
            }

            var companyId = preference.CompanyId;
            _logger.LogInformation(
                "Running QuickBooks sync for company {CompanyId} at frequency {Frequency}.",
                companyId,
                preference.SyncFrequency);

            var invoiceCount = 0;
            var batchCount = 0;

            if (preference.AutoSyncInvoices)
            {
                invoiceCount = await SyncInvoicesAsync(companyId, invoiceSyncService, dbContext, cancellationToken);
            }

            if (preference.AutoSyncCogs)
            {
                batchCount = await SyncBatchCostsAsync(companyId, costTrackingService, dbContext, cancellationToken);
            }

            preference.LastSyncAt = DateTime.UtcNow;
            preference.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "QuickBooks sync completed for company {CompanyId}. Invoices processed: {InvoiceCount}. Batches processed: {BatchCount}.",
                companyId,
                invoiceCount,
                batchCount);
        }

        return CalculateNextDelay(preferences);
    }

    private static bool ShouldProcessPreference(AccountingSyncPreference preference)
    {
        if (!preference.AutoSyncInvoices && !preference.AutoSyncCogs)
        {
            return false;
        }

        var interval = GetFrequencyInterval(preference.SyncFrequency);
        if (interval == Timeout.InfiniteTimeSpan)
        {
            return false;
        }

        if (interval == TimeSpan.Zero)
        {
            return true;
        }

        if (preference.LastSyncAt is null)
        {
            return true;
        }

        return DateTime.UtcNow - preference.LastSyncAt >= interval;
    }

    private static TimeSpan GetFrequencyInterval(string? frequency)
    {
        return frequency?.ToLowerInvariant() switch
        {
            "hourly" => HourlyInterval,
            "daily" => DailyInterval,
            "immediate" => TimeSpan.Zero,
            _ => Timeout.InfiniteTimeSpan
        };
    }

    private TimeSpan CalculateNextDelay(IReadOnlyCollection<AccountingSyncPreference> preferences)
    {
        if (preferences.Count == 0)
        {
            return DefaultPollingInterval;
        }

        var now = DateTime.UtcNow;
        var dueIntervals = new List<TimeSpan>();

        foreach (var preference in preferences)
        {
            if (!preference.AutoSyncInvoices && !preference.AutoSyncCogs)
            {
                continue;
            }

            var interval = GetFrequencyInterval(preference.SyncFrequency);
            if (interval == Timeout.InfiniteTimeSpan)
            {
                continue;
            }

            var lastSync = preference.LastSyncAt ?? DateTime.MinValue;
            var nextRun = interval == TimeSpan.Zero ? now : lastSync + interval;
            var delay = nextRun <= now ? TimeSpan.Zero : nextRun - now;
            dueIntervals.Add(delay);
        }

        if (dueIntervals.Count == 0)
        {
            return DefaultPollingInterval;
        }

        var minDelay = dueIntervals.Min();
        return minDelay <= TimeSpan.Zero ? MinimumDelay : minDelay;
    }

    private async Task<int> SyncInvoicesAsync(
        int companyId,
        IQuickBooksInvoiceSyncService invoiceSyncService,
        CaskrDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pendingInvoices = await GetPendingLogsAsync(dbContext, companyId, InvoiceEntityType, cancellationToken);
        var processed = 0;

        foreach (var log in pendingInvoices)
        {
            if (!int.TryParse(log.EntityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var invoiceId))
            {
                continue;
            }

            processed++;
            try
            {
                var result = await invoiceSyncService.SyncInvoiceToQBOAsync(invoiceId);
                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Invoice {InvoiceId} for company {CompanyId} failed to sync to QuickBooks: {Error}",
                        invoiceId,
                        companyId,
                        result.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while syncing invoice {InvoiceId} for company {CompanyId}.",
                    invoiceId,
                    companyId);
            }
        }

        return processed;
    }

    private async Task<int> SyncBatchCostsAsync(
        int companyId,
        IQuickBooksCostTrackingService costTrackingService,
        CaskrDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pendingBatches = await GetPendingLogsAsync(dbContext, companyId, BatchEntityType, cancellationToken);
        var processed = 0;

        foreach (var log in pendingBatches)
        {
            if (!int.TryParse(log.EntityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var batchId))
            {
                continue;
            }

            processed++;
            try
            {
                var result = await costTrackingService.RecordBatchCOGSAsync(batchId);
                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Batch {BatchId} for company {CompanyId} failed to sync to QuickBooks: {Error}",
                        batchId,
                        companyId,
                        result.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while syncing batch {BatchId} for company {CompanyId}.",
                    batchId,
                    companyId);
            }
        }

        return processed;
    }

    private static async Task<List<AccountingSyncLog>> GetPendingLogsAsync(
        CaskrDbContext dbContext,
        int companyId,
        string entityType,
        CancellationToken cancellationToken)
    {
        return await dbContext.AccountingSyncLogs
            .AsNoTracking()
            .Where(log => log.CompanyId == companyId
                          && log.EntityType == entityType
                          && (log.SyncStatus == SyncStatus.Pending || log.SyncStatus == SyncStatus.Failed)
                          && log.RetryCount < MaxRetryCount)
            .OrderBy(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
