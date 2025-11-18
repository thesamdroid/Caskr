using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.Server.Events;
using Caskr.Server.Services;
using Caskr.server.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

/// <summary>
///     Responds to <see cref="BatchCompletedEvent" /> notifications by dispatching COGS journal entry
///     jobs to the background worker so QuickBooks stays in sync without blocking UI requests.
/// </summary>
public sealed class QuickBooksBatchCostTrackingEventHandler : INotificationHandler<BatchCompletedEvent>
{
    private readonly CaskrDbContext _dbContext;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<QuickBooksBatchCostTrackingEventHandler> _logger;

    public QuickBooksBatchCostTrackingEventHandler(
        CaskrDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<QuickBooksBatchCostTrackingEventHandler> logger)
    {
        _dbContext = dbContext;
        _taskQueue = taskQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task Handle(BatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        var hasQuickBooksIntegration = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .AnyAsync(
                ai => ai.CompanyId == notification.CompanyId
                      && ai.Provider == AccountingProvider.QuickBooks
                      && ai.IsActive,
                cancellationToken);

        if (!hasQuickBooksIntegration)
        {
            _logger.LogInformation(
                "Batch {BatchId} completed but QuickBooks is not connected for company {CompanyId}. Skipping COGS sync.",
                notification.BatchId,
                notification.CompanyId);
            return;
        }

        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var costTrackingService = scope.ServiceProvider.GetRequiredService<IQuickBooksCostTrackingService>();
            try
            {
                var result = await costTrackingService.RecordBatchCOGSAsync(notification.BatchId);
                if (!result.Success)
                {
                    _logger.LogWarning(
                        "QuickBooks COGS sync for batch {BatchId} failed: {Error}",
                        notification.BatchId,
                        result.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "QuickBooks COGS sync for batch {BatchId} threw an exception.",
                    notification.BatchId);
            }
        });
    }
}
