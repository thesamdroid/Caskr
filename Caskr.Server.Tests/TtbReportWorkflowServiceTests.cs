using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public sealed class TtbReportWorkflowServiceTests : IDisposable
{
    private readonly CaskrDbContext dbContext;
    private readonly Mock<IEmailService> emailService = new();
    private readonly Mock<ITtbAuditLogger> auditLogger = new();
    private readonly Mock<IUsersService> usersService = new();
    private readonly Mock<IWebhookService> webhookService = new();

    public TtbReportWorkflowServiceTests()
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

        // Regular user
        dbContext.Users.Add(new User
        {
            Id = 1,
            CompanyId = 1,
            Email = "user@distillery.com",
            Name = "Test User",
            UserTypeId = (int)UserTypeEnum.Distiller,
            IsPrimaryContact = true
        });

        // Compliance Manager
        dbContext.Users.Add(new User
        {
            Id = 2,
            CompanyId = 1,
            Email = "compliance@distillery.com",
            Name = "Compliance Manager",
            UserTypeId = (int)UserTypeEnum.ComplianceManager,
            IsPrimaryContact = false,
            IsTtbContact = true
        });

        // Admin user
        dbContext.Users.Add(new User
        {
            Id = 3,
            CompanyId = 1,
            Email = "admin@distillery.com",
            Name = "Admin User",
            UserTypeId = (int)UserTypeEnum.Admin,
            IsPrimaryContact = false
        });

        dbContext.SaveChanges();

        // Setup users service mocks
        usersService.Setup(u => u.GetUserByIdAsync(1))
            .ReturnsAsync(dbContext.Users.Find(1));
        usersService.Setup(u => u.GetUserByIdAsync(2))
            .ReturnsAsync(dbContext.Users.Find(2));
        usersService.Setup(u => u.GetUserByIdAsync(3))
            .ReturnsAsync(dbContext.Users.Find(3));
    }

    private TtbReportWorkflowService CreateService()
    {
        webhookService.Setup(w => w.TriggerEventAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        return new TtbReportWorkflowService(
            dbContext,
            emailService.Object,
            auditLogger.Object,
            usersService.Object,
            webhookService.Object,
            NullLogger<TtbReportWorkflowService>.Instance);
    }

    [Fact]
    public async Task SubmitForReviewAsync_DraftReport_SucceedsAndChangesStatus()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 100,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            GeneratedAt = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitForReviewAsync(100, 1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(TtbReportStatus.PendingReview, result.Report.Status);
        Assert.Equal(1, result.Report.SubmittedForReviewByUserId);
        Assert.NotNull(result.Report.SubmittedForReviewAt);

        // Verify audit log was called
        auditLogger.Verify(a => a.LogChangeAsync(
            TtbAuditAction.Update,
            It.IsAny<TtbMonthlyReport>(),
            It.IsAny<TtbMonthlyReport>(),
            1,
            1), Times.Once);
    }

    [Fact]
    public async Task SubmitForReviewAsync_ReportWithValidationErrors_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 101,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            ValidationErrors = "[\"Missing TTB permit number\"]"
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitForReviewAsync(101, 1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("validation error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitForReviewAsync_NonDraftReport_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 102,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitForReviewAsync(102, 1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be submitted for review", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveReportAsync_PendingReviewReport_WithComplianceManager_Succeeds()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 103,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            SubmittedForReviewByUserId = 1,
            SubmittedForReviewAt = DateTime.UtcNow.AddHours(-2)
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ApproveReportAsync(103, 2, "Looks good!");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(TtbReportStatus.Approved, result.Report.Status);
        Assert.Equal(2, result.Report.ReviewedByUserId);
        Assert.Equal(2, result.Report.ApprovedByUserId);
        Assert.NotNull(result.Report.ReviewedAt);
        Assert.NotNull(result.Report.ApprovedAt);
        Assert.Equal("Looks good!", result.Report.ReviewNotes);
    }

    [Fact]
    public async Task ApproveReportAsync_WithRegularUser_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 104,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ApproveReportAsync(104, 1); // User 1 is Distiller

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Compliance Manager", result.ErrorMessage);
    }

    [Fact]
    public async Task ApproveReportAsync_NonPendingReviewReport_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 105,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ApproveReportAsync(105, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be approved", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectReportAsync_PendingReviewReport_ReturnsToDraft()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 106,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            SubmittedForReviewByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.RejectReportAsync(106, 2, "Missing transaction data");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(TtbReportStatus.Draft, result.Report.Status);
        Assert.Equal(2, result.Report.ReviewedByUserId);
        Assert.Null(result.Report.ApprovedByUserId);
        Assert.Equal("Missing transaction data", result.Report.ReviewNotes);
    }

    [Fact]
    public async Task SubmitToTtbAsync_ApprovedReport_SucceedsAndLocksData()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 107,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            ApprovedByUserId = 2,
            ApprovedAt = DateTime.UtcNow.AddHours(-1)
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitToTtbAsync(107, 1, "TTB-2024-1015-001");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(TtbReportStatus.Submitted, result.Report.Status);
        Assert.Equal("TTB-2024-1015-001", result.Report.TtbConfirmationNumber);
        Assert.NotNull(result.Report.SubmittedAt);
    }

    [Fact]
    public async Task SubmitToTtbAsync_WithEmptyConfirmationNumber_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 108,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitToTtbAsync(108, 1, "");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("confirmation number", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitToTtbAsync_WithValidationErrors_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 214,
            CompanyId = 1,
            ReportMonth = 11,
            ReportYear = 2024,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            ApprovedByUserId = 2,
            ValidationErrors = "[\"Storage data missing\"]"
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitToTtbAsync(214, 1, "TTB-99999");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("validation error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitToTtbAsync_NonApprovedReport_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 109,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.SubmitToTtbAsync(109, 1, "TTB-12345");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be submitted to TTB", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArchiveReportAsync_SubmittedReport_Succeeds()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 110,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Submitted,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            TtbConfirmationNumber = "TTB-12345",
            SubmittedAt = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ArchiveReportAsync(110, 1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal(TtbReportStatus.Archived, result.Report.Status);
    }

    [Fact]
    public async Task ArchiveReportAsync_NonSubmittedReport_Fails()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 111,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.Approved,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ArchiveReportAsync(111, 1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be archived", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReviewersForCompanyAsync_ReturnsComplianceManagersAndAdmins()
    {
        // Arrange
        var service = CreateService();

        // Act
        var reviewers = await service.GetReviewersForCompanyAsync(1);

        // Assert
        var reviewerList = reviewers.ToList();
        Assert.Equal(2, reviewerList.Count);
        Assert.Contains(reviewerList, r => r.Id == 2); // Compliance Manager
        Assert.Contains(reviewerList, r => r.Id == 3); // Admin
        Assert.DoesNotContain(reviewerList, r => r.Id == 1); // Regular Distiller
    }

    [Fact]
    public async Task SubmitForReviewAsync_SendsEmailToReviewers()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 112,
            CompanyId = 1,
            ReportMonth = 11,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.SubmitForReviewAsync(112, 1);

        // Assert - emails sent to compliance manager and admin
        emailService.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Review Requested")),
            It.IsAny<string>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task ApproveReportAsync_WithAdmin_Succeeds()
    {
        // Arrange
        var report = new TtbMonthlyReport
        {
            Id = 113,
            CompanyId = 1,
            ReportMonth = 10,
            ReportYear = 2024,
            Status = TtbReportStatus.PendingReview,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1,
            SubmittedForReviewByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act - Admin user (ID 3) approves
        var result = await service.ApproveReportAsync(113, 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TtbReportStatus.Approved, result.Report!.Status);
    }

    [Fact]
    public async Task SubmitForReviewAsync_ReportNotFound_Fails()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SubmitForReviewAsync(999, 1);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Report not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteWorkflow_DraftToArchived_Succeeds()
    {
        // Arrange - Create draft report
        var report = new TtbMonthlyReport
        {
            Id = 114,
            CompanyId = 1,
            ReportMonth = 12,
            ReportYear = 2024,
            Status = TtbReportStatus.Draft,
            FormType = TtbFormType.Form5110_28,
            CreatedByUserId = 1
        };
        dbContext.TtbMonthlyReports.Add(report);
        await dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act 1: Submit for review
        var submitResult = await service.SubmitForReviewAsync(114, 1);
        Assert.True(submitResult.Success);
        Assert.Equal(TtbReportStatus.PendingReview, submitResult.Report!.Status);

        // Act 2: Approve
        var approveResult = await service.ApproveReportAsync(114, 2);
        Assert.True(approveResult.Success);
        Assert.Equal(TtbReportStatus.Approved, approveResult.Report!.Status);

        // Act 3: Submit to TTB
        var ttbResult = await service.SubmitToTtbAsync(114, 1, "TTB-2024-12345");
        Assert.True(ttbResult.Success);
        Assert.Equal(TtbReportStatus.Submitted, ttbResult.Report!.Status);

        // Act 4: Archive
        var archiveResult = await service.ArchiveReportAsync(114, 1);
        Assert.True(archiveResult.Success);
        Assert.Equal(TtbReportStatus.Archived, archiveResult.Report!.Status);

        // Verify final state
        var finalReport = await dbContext.TtbMonthlyReports.FindAsync(114);
        Assert.NotNull(finalReport);
        Assert.Equal(TtbReportStatus.Archived, finalReport.Status);
        Assert.Equal("TTB-2024-12345", finalReport.TtbConfirmationNumber);
        Assert.NotNull(finalReport.SubmittedAt);
        Assert.NotNull(finalReport.ApprovedAt);
        Assert.NotNull(finalReport.SubmittedForReviewAt);
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }
}
