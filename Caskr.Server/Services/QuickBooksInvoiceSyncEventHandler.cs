using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.Server.Events;
using Caskr.Server.Services;
using Caskr.server.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

/// <summary>
///     Handles <see cref="OrderCompletedEvent" /> events by dispatching QuickBooks invoice sync jobs to the background queue.
/// </summary>
public class QuickBooksInvoiceSyncEventHandler : INotificationHandler<OrderCompletedEvent>
{
    private readonly CaskrDbContext _dbContext;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IQuickBooksInvoiceSyncService _invoiceSyncService;
    private readonly ILogger<QuickBooksInvoiceSyncEventHandler> _logger;

    public QuickBooksInvoiceSyncEventHandler(
        CaskrDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IQuickBooksInvoiceSyncService invoiceSyncService,
        ILogger<QuickBooksInvoiceSyncEventHandler> logger)
    {
        _dbContext = dbContext;
        _taskQueue = taskQueue;
        _invoiceSyncService = invoiceSyncService;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.InvoiceId is null)
        {
            _logger.LogDebug(
                "Order {OrderId} completed without an invoice. QuickBooks sync not required.",
                notification.OrderId);
            return;
        }

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
                "Order {OrderId} completed but QuickBooks is not connected for company {CompanyId}. Skipping sync.",
                notification.OrderId,
                notification.CompanyId);
            return;
        }

        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            try
            {
                var result = await _invoiceSyncService.SyncInvoiceToQBOAsync(notification.InvoiceId.Value);
                if (!result.Success)
                {
                    _logger.LogWarning(
                        "QuickBooks invoice sync for order {OrderId} failed: {Error}",
                        notification.OrderId,
                        result.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "QuickBooks invoice sync for order {OrderId} threw an exception.",
                    notification.OrderId);
            }
        });
    }
}
