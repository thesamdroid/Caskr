using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.Server.Events;
using Caskr.Server.Services;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksBatchCostTrackingEventHandlerTests
{
    private static CaskrDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new CaskrDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenQuickBooksConnected_EnqueuesWork()
    {
        await using var context = CreateContext(nameof(Handle_WhenQuickBooksConnected_EnqueuesWork));
        context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = 9,
            Provider = AccountingProvider.QuickBooks,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var queue = new Mock<IBackgroundTaskQueue>();
        queue.Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()))
            .Returns(ValueTask.CompletedTask);
        var costService = new Mock<IQuickBooksCostTrackingService>();
        costService.Setup(s => s.RecordBatchCOGSAsync(It.IsAny<int>()))
            .ReturnsAsync(new JournalEntrySyncResult(true, "J-1", null));
        var logger = Mock.Of<ILogger<QuickBooksBatchCostTrackingEventHandler>>();
        var handler = new QuickBooksBatchCostTrackingEventHandler(context, queue.Object, costService.Object, logger);

        await handler.Handle(new BatchCompletedEvent(55, 9), CancellationToken.None);

        queue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQuickBooksNotConnected_DoesNotEnqueue()
    {
        await using var context = CreateContext(nameof(Handle_WhenQuickBooksNotConnected_DoesNotEnqueue));
        var queue = new Mock<IBackgroundTaskQueue>();
        var costService = new Mock<IQuickBooksCostTrackingService>();
        var logger = Mock.Of<ILogger<QuickBooksBatchCostTrackingEventHandler>>();
        var handler = new QuickBooksBatchCostTrackingEventHandler(context, queue.Object, costService.Object, logger);

        await handler.Handle(new BatchCompletedEvent(10, 2), CancellationToken.None);

        queue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Never);
    }
}
