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

namespace Caskr.Server.Tests;

public class QuickBooksInvoiceSyncEventHandlerTests
{
    private static CaskrDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new CaskrDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenQuickBooksConnected_EnqueuesBackgroundWork()
    {
        await using var context = CreateContext(nameof(Handle_WhenQuickBooksConnected_EnqueuesBackgroundWork));
        context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = 1,
            Provider = AccountingProvider.QuickBooks,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var queue = new Mock<IBackgroundTaskQueue>();
        queue.Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()))
            .Returns(ValueTask.CompletedTask);
        var syncService = new Mock<IQuickBooksInvoiceSyncService>();
        syncService.Setup(s => s.SyncInvoiceToQBOAsync(It.IsAny<int>()))
            .ReturnsAsync(new InvoiceSyncResult(true, "QBO-1", null));
        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncEventHandler>>();
        var handler = new QuickBooksInvoiceSyncEventHandler(context, queue.Object, syncService.Object, logger);

        await handler.Handle(new OrderCompletedEvent(10, 1, 5), CancellationToken.None);

        queue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQuickBooksNotConnected_DoesNotEnqueue()
    {
        await using var context = CreateContext(nameof(Handle_WhenQuickBooksNotConnected_DoesNotEnqueue));
        var queue = new Mock<IBackgroundTaskQueue>();
        var syncService = new Mock<IQuickBooksInvoiceSyncService>();
        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncEventHandler>>();
        var handler = new QuickBooksInvoiceSyncEventHandler(context, queue.Object, syncService.Object, logger);

        await handler.Handle(new OrderCompletedEvent(10, 1, 5), CancellationToken.None);

        queue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutInvoice_DoesNotEnqueue()
    {
        await using var context = CreateContext(nameof(Handle_WithoutInvoice_DoesNotEnqueue));
        var queue = new Mock<IBackgroundTaskQueue>();
        var syncService = new Mock<IQuickBooksInvoiceSyncService>();
        var logger = Mock.Of<ILogger<QuickBooksInvoiceSyncEventHandler>>();
        var handler = new QuickBooksInvoiceSyncEventHandler(context, queue.Object, syncService.Object, logger);

        await handler.Handle(new OrderCompletedEvent(10, 1, null), CancellationToken.None);

        queue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Never);
    }
}
