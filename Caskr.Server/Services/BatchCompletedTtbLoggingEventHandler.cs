using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.Server.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Listens for <see cref="BatchCompletedEvent"/> notifications and ensures the production run is recorded in
///     the TTB transaction log whenever compliance tracking is enabled for the company.
/// </summary>
public sealed class BatchCompletedTtbLoggingEventHandler(
    CaskrDbContext dbContext,
    ITtbTransactionLogger ttbTransactionLogger,
    ILogger<BatchCompletedTtbLoggingEventHandler> logger) : INotificationHandler<BatchCompletedEvent>
{
    public async Task Handle(BatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (!await IsTtbComplianceEnabledAsync(notification.CompanyId, cancellationToken))
        {
            logger.LogDebug(
                "TTB compliance is not enabled for company {CompanyId}. Skipping production logging for batch {BatchId}.",
                notification.CompanyId,
                notification.BatchId);

            return;
        }

        try
        {
            await ttbTransactionLogger.LogProductionAsync(notification.BatchId, DateTime.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to log TTB production transaction for batch {BatchId} belonging to company {CompanyId}.",
                notification.BatchId,
                notification.CompanyId);
        }
    }

    private Task<bool> IsTtbComplianceEnabledAsync(int companyId, CancellationToken cancellationToken)
    {
        return dbContext.TtbMonthlyReports
            .AsNoTracking()
            .AnyAsync(report => report.CompanyId == companyId, cancellationToken);
    }
}
