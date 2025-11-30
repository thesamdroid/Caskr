using System;
using System.Linq;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public sealed class TtbAuditLoggerServiceTests : IDisposable
{
    private readonly CaskrDbContext dbContext;
    private readonly Mock<IHttpContextAccessor> httpContextAccessor = new();
    private readonly Mock<IUsersService> usersService = new();

    public TtbAuditLoggerServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbContext = new CaskrDbContext(options);

        // Set up test data
        dbContext.Companies.Add(new Company
        {
            Id = 1,
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });

        dbContext.Users.Add(new User
        {
            Id = 1,
            CompanyId = 1,
            Email = "test@distillery.com",
            Name = "Test User",
            UserTypeId = 2,
            IsPrimaryContact = true
        });

        dbContext.SaveChanges();

        // Setup HTTP context
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Setup users service
        usersService.Setup(u => u.GetUserByIdAsync(1))
            .ReturnsAsync(dbContext.Users.First());
    }

    private TtbAuditLoggerService CreateService()
    {
        return new TtbAuditLoggerService(
            dbContext,
            httpContextAccessor.Object,
            NullLogger<TtbAuditLoggerService>.Instance,
            usersService.Object);
    }

    [Fact]
    public async Task LogChangeAsync_CreateAction_CreatesAuditLogEntry()
    {
        // Arrange
        var service = CreateService();
        var transaction = new TtbTransaction
        {
            Id = 100,
            CompanyId = 1,
            TransactionDate = new DateTime(2024, 10, 15),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 50.00m,
            WineGallons = 25.00m,
            Notes = "Test production"
        };

        // Act
        await service.LogChangeAsync(TtbAuditAction.Create, transaction, null, 1, 1);

        // Assert
        var auditLog = await dbContext.TtbAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(1, auditLog.CompanyId);
        Assert.Equal("TtbTransaction", auditLog.EntityType);
        Assert.Equal(100, auditLog.EntityId);
        Assert.Equal(TtbAuditAction.Create, auditLog.Action);
        Assert.Equal(1, auditLog.ChangedByUserId);
        Assert.Null(auditLog.OldValues);
        Assert.NotNull(auditLog.NewValues);
        Assert.Equal("192.168.1.1", auditLog.IpAddress);
        Assert.Equal("TestAgent/1.0", auditLog.UserAgent);
        Assert.NotNull(auditLog.ChangeDescription);
        Assert.Contains("created", auditLog.ChangeDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogChangeAsync_UpdateAction_CapturesOldAndNewValues()
    {
        // Arrange
        var service = CreateService();
        var oldTransaction = new TtbTransaction
        {
            Id = 100,
            CompanyId = 1,
            TransactionDate = new DateTime(2024, 10, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 5.00m,
            WineGallons = 2.50m,
            Notes = "Original loss"
        };

        var newTransaction = new TtbTransaction
        {
            Id = 100,
            CompanyId = 1,
            TransactionDate = new DateTime(2024, 10, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 7.50m,
            WineGallons = 3.75m,
            Notes = "Updated loss"
        };

        // Act
        await service.LogChangeAsync(TtbAuditAction.Update, newTransaction, oldTransaction, 1, 1);

        // Assert
        var auditLog = await dbContext.TtbAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(TtbAuditAction.Update, auditLog.Action);
        Assert.NotNull(auditLog.OldValues);
        Assert.NotNull(auditLog.NewValues);
        Assert.Contains("5.00", auditLog.OldValues);
        Assert.Contains("7.50", auditLog.NewValues);
        Assert.Contains("updated", auditLog.ChangeDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogChangeAsync_DeleteAction_CapturesOldValues()
    {
        // Arrange
        var service = CreateService();
        var deletedTransaction = new TtbTransaction
        {
            Id = 100,
            CompanyId = 1,
            TransactionDate = new DateTime(2024, 10, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 5.00m,
            WineGallons = 2.50m,
            Notes = "Deleted loss"
        };

        // Act
        await service.LogChangeAsync(TtbAuditAction.Delete, null, deletedTransaction, 1, 1);

        // Assert
        var auditLog = await dbContext.TtbAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(TtbAuditAction.Delete, auditLog.Action);
        Assert.NotNull(auditLog.OldValues);
        Assert.Null(auditLog.NewValues);
        Assert.Contains("deleted", auditLog.ChangeDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithSubmittedReport_ReturnsTrue()
    {
        // Arrange
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Submitted,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithApprovedReport_ReturnsTrue()
    {
        // Arrange
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithDraftReport_ReturnsFalse()
    {
        // Arrange
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithNoReport_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithArchivedReport_ReturnsTrue()
    {
        // Arrange
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Archived,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            TtbConfirmationNumber = "TTB-12345"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public async Task IsMonthLockedAsync_WithPendingReviewReport_ReturnsFalse()
    {
        // Arrange
        dbContext.TtbMonthlyReports.Add(new TtbMonthlyReport
        {
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var isLocked = await service.IsMonthLockedAsync(1, 10, 2024);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public async Task ExportAuditLogsToCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        dbContext.TtbAuditLogs.Add(new TtbAuditLog
        {
            CompanyId = 1,
            EntityType = "TtbTransaction",
            EntityId = 100,
            Action = TtbAuditAction.Create,
            ChangedByUserId = 1,
            ChangeTimestamp = new DateTime(2024, 10, 15, 10, 30, 0, DateTimeKind.Utc),
            IpAddress = "192.168.1.1",
            ChangeDescription = "Test User created Production transaction for 50.00 proof gallons on 10/15/2024"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var csv = await service.ExportAuditLogsToCsvAsync(1, null, null);

        // Assert
        Assert.NotEmpty(csv);
        Assert.Contains("Timestamp (UTC)", csv);
        Assert.Contains("User", csv);
        Assert.Contains("Entity Type", csv);
        Assert.Contains("TtbTransaction", csv);
        Assert.Contains("Create", csv);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsLogsForCompany()
    {
        // Arrange
        dbContext.TtbAuditLogs.AddRange(
            new TtbAuditLog
            {
                CompanyId = 1,
                EntityType = "TtbTransaction",
                EntityId = 100,
                Action = TtbAuditAction.Create,
                ChangedByUserId = 1,
                ChangeTimestamp = new DateTime(2024, 10, 15, 10, 30, 0, DateTimeKind.Utc)
            },
            new TtbAuditLog
            {
                CompanyId = 1,
                EntityType = "TtbTransaction",
                EntityId = 101,
                Action = TtbAuditAction.Update,
                ChangedByUserId = 1,
                ChangeTimestamp = new DateTime(2024, 10, 16, 11, 30, 0, DateTimeKind.Utc)
            },
            new TtbAuditLog
            {
                CompanyId = 2,
                EntityType = "TtbTransaction",
                EntityId = 200,
                Action = TtbAuditAction.Create,
                ChangedByUserId = 2,
                ChangeTimestamp = new DateTime(2024, 10, 15, 10, 30, 0, DateTimeKind.Utc)
            }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var logs = await service.GetAuditLogsAsync(1, null, null);

        // Assert
        var logList = logs.ToList();
        Assert.Equal(2, logList.Count);
        Assert.All(logList, l => Assert.Equal(1, l.CompanyId));
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByDateRange()
    {
        // Arrange
        dbContext.TtbAuditLogs.AddRange(
            new TtbAuditLog
            {
                CompanyId = 1,
                EntityType = "TtbTransaction",
                EntityId = 100,
                Action = TtbAuditAction.Create,
                ChangedByUserId = 1,
                ChangeTimestamp = new DateTime(2024, 10, 10, 10, 30, 0, DateTimeKind.Utc)
            },
            new TtbAuditLog
            {
                CompanyId = 1,
                EntityType = "TtbTransaction",
                EntityId = 101,
                Action = TtbAuditAction.Update,
                ChangedByUserId = 1,
                ChangeTimestamp = new DateTime(2024, 10, 15, 11, 30, 0, DateTimeKind.Utc)
            },
            new TtbAuditLog
            {
                CompanyId = 1,
                EntityType = "TtbTransaction",
                EntityId = 102,
                Action = TtbAuditAction.Delete,
                ChangedByUserId = 1,
                ChangeTimestamp = new DateTime(2024, 10, 20, 12, 30, 0, DateTimeKind.Utc)
            }
        );
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var logs = await service.GetAuditLogsAsync(
            1,
            new DateTime(2024, 10, 12),
            new DateTime(2024, 10, 18));

        // Assert
        var logList = logs.ToList();
        Assert.Single(logList);
        Assert.Equal(101, logList[0].EntityId);
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }
}
