using Caskr.server.Models;
using Caskr.Server.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Events;

/// <summary>
/// MediatR notification handler that triggers webhooks for domain events.
/// </summary>
public class WebhookEventHandler :
    INotificationHandler<OrderCompletedEvent>,
    INotificationHandler<BatchCompletedEvent>
{
    private readonly IWebhookService _webhookService;
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<WebhookEventHandler> _logger;

    public WebhookEventHandler(
        IWebhookService webhookService,
        CaskrDbContext dbContext,
        ILogger<WebhookEventHandler> logger)
    {
        _webhookService = webhookService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Handles order completed events by triggering the order.completed webhook.
    /// </summary>
    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _dbContext.Orders.FindAsync(notification.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for webhook trigger", notification.OrderId);
                return;
            }

            await _webhookService.TriggerEventAsync(
                WebhookEventTypes.OrderCompleted,
                notification.OrderId,
                new
                {
                    id = order.Id,
                    name = order.Name,
                    status_id = order.StatusId,
                    owner_id = order.OwnerId,
                    batch_id = order.BatchId,
                    invoice_id = order.InvoiceId,
                    quantity = order.Quantity,
                    company_id = notification.CompanyId
                },
                notification.CompanyId);

            _logger.LogInformation(
                "Triggered order.completed webhook for order {OrderId} in company {CompanyId}",
                notification.OrderId,
                notification.CompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhook for order completed event. OrderId: {OrderId}", notification.OrderId);
        }
    }

    /// <summary>
    /// Handles batch completed events by triggering the batch.completed webhook.
    /// </summary>
    public async Task Handle(BatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await _dbContext.Batches.FindAsync(notification.BatchId, notification.CompanyId);
            if (batch == null)
            {
                _logger.LogWarning("Batch {BatchId} not found for webhook trigger", notification.BatchId);
                return;
            }

            await _webhookService.TriggerEventAsync(
                WebhookEventTypes.BatchCompleted,
                notification.BatchId,
                new
                {
                    id = batch.Id,
                    mash_bill_id = batch.MashBillId,
                    company_id = notification.CompanyId
                },
                notification.CompanyId);

            _logger.LogInformation(
                "Triggered batch.completed webhook for batch {BatchId} in company {CompanyId}",
                notification.BatchId,
                notification.CompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhook for batch completed event. BatchId: {BatchId}", notification.BatchId);
        }
    }
}
