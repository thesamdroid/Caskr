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

public class BatchCompletedTtbLoggingEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenTtbComplianceDisabled_DoesNotLogProduction()
    {
        await using var context = CreateContext(nameof(Handle_WhenTtbComplianceDisabled_DoesNotLogProduction));
        var ttbLogger = new Mock<ITtbTransactionLogger>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<BatchCompletedTtbLoggingEventHandler>>();
        var handler = new BatchCompletedTtbLoggingEventHandler(context, ttbLogger.Object, logger);

        await handler.Handle(new BatchCompletedEvent(10, 5), CancellationToken.None);

        ttbLogger.Verify(l => l.LogProductionAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTtbComplianceEnabled_LogsProductionOnce()
    {
        await using var context = CreateContext(nameof(Handle_WhenTtbComplianceEnabled_LogsProductionOnce));
        context.Users.Add(new User
        {
            Id = 7,
            CompanyId = 3,
            Email = "user@example.com",
            Name = "Test User",
            UserTypeId = 1
        });
        context.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 3,
            ReportMonth = 1,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            CreatedByUserId = 7
        });
        await context.SaveChangesAsync();

        var ttbLogger = new Mock<ITtbTransactionLogger>();
        var logger = Mock.Of<ILogger<BatchCompletedTtbLoggingEventHandler>>();
        var handler = new BatchCompletedTtbLoggingEventHandler(context, ttbLogger.Object, logger);

        await handler.Handle(new BatchCompletedEvent(42, 3), CancellationToken.None);

        ttbLogger.Verify(l => l.LogProductionAsync(42, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoggingFails_DoesNotPropagateException()
    {
        await using var context = CreateContext(nameof(Handle_WhenLoggingFails_DoesNotPropagateException));
        context.Users.Add(new User
        {
            Id = 8,
            CompanyId = 11,
            Email = "user@example.com",
            Name = "Test User",
            UserTypeId = 1
        });
        context.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 11,
            ReportMonth = 2,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            CreatedByUserId = 8
        });
        await context.SaveChangesAsync();

        var ttbLogger = new Mock<ITtbTransactionLogger>();
        ttbLogger
            .Setup(l => l.LogProductionAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));
        var logger = Mock.Of<ILogger<BatchCompletedTtbLoggingEventHandler>>();
        var handler = new BatchCompletedTtbLoggingEventHandler(context, ttbLogger.Object, logger);

        await handler.Handle(new BatchCompletedEvent(99, 11), CancellationToken.None);

        ttbLogger.Verify(l => l.LogProductionAsync(99, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoggerDoesNotPersist_SavesChangesToContext()
    {
        const int companyId = 21;
        const int batchId = 77;

        await using var context = CreateContext(nameof(Handle_WhenLoggerDoesNotPersist_SavesChangesToContext));
        context.Users.Add(new User
        {
            Id = 33,
            CompanyId = companyId,
            Email = "user@example.com",
            Name = "Test User",
            UserTypeId = 1
        });
        context.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = companyId,
            ReportMonth = 3,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            CreatedByUserId = 33
        });
        await context.SaveChangesAsync();

        var ttbLogger = new NonPersistingTtbTransactionLogger(context, companyId);
        var logger = Mock.Of<ILogger<BatchCompletedTtbLoggingEventHandler>>();
        var handler = new BatchCompletedTtbLoggingEventHandler(context, ttbLogger, logger);

        await handler.Handle(new BatchCompletedEvent(batchId, companyId), CancellationToken.None);

        var transaction = await context.TtbTransactions.SingleOrDefaultAsync(t => t.SourceEntityId == batchId);
        Assert.NotNull(transaction);
        Assert.Equal(companyId, transaction!.CompanyId);
        Assert.Equal(TtbTransactionType.Production, transaction.TransactionType);
    }

    private static CaskrDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new CaskrDbContext(options);
    }

    private sealed class NonPersistingTtbTransactionLogger(CaskrDbContext context, int companyId) : ITtbTransactionLogger
    {
        public Task LogProductionAsync(int batchId, DateTime productionDate)
        {
            context.TtbTransactions.Add(new TtbTransaction
            {
                CompanyId = companyId,
                TransactionDate = productionDate,
                TransactionType = TtbTransactionType.Production,
                ProductType = "Test Product",
                SpiritsType = TtbSpiritsType.Under190Proof,
                SourceEntityType = nameof(Batch),
                SourceEntityId = batchId
            });

            return Task.CompletedTask;
        }

        public Task LogTransferInAsync(int transferId) => throw new NotSupportedException();

        public Task LogTransferOutAsync(int transferId) => throw new NotSupportedException();

        public Task LogLossAsync(int barrelId, decimal proofGallons, string reason) => throw new NotSupportedException();

        public Task LogTaxDeterminationAsync(int orderId) => throw new NotSupportedException();
    }
}
