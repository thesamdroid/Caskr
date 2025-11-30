using System.Text.Json;
using Caskr.server.Models;
using Caskr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

/// <summary>
/// Result of a workflow transition operation.
/// </summary>
public class WorkflowTransitionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TtbMonthlyReport? Report { get; init; }

    public static WorkflowTransitionResult Succeeded(TtbMonthlyReport report) => new()
    {
        Success = true,
        Report = report
    };

    public static WorkflowTransitionResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Interface for TTB report workflow operations.
/// </summary>
public interface ITtbReportWorkflowService
{
    /// <summary>
    /// Submits a report for review (Draft → PendingReview).
    /// Validates the report before allowing transition.
    /// </summary>
    Task<WorkflowTransitionResult> SubmitForReviewAsync(int reportId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a report for TTB submission (PendingReview → Approved).
    /// Requires the user to have ComplianceManager role.
    /// </summary>
    Task<WorkflowTransitionResult> ApproveReportAsync(int reportId, int userId, string? reviewNotes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a report back to draft status (PendingReview → Draft).
    /// Requires the user to have ComplianceManager role.
    /// </summary>
    Task<WorkflowTransitionResult> RejectReportAsync(int reportId, int userId, string? reviewNotes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a report as submitted to TTB (Approved → Submitted).
    /// Records the TTB confirmation number and locks all related data.
    /// </summary>
    Task<WorkflowTransitionResult> SubmitToTtbAsync(int reportId, int userId, string confirmationNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a submitted report (Submitted → Archived).
    /// </summary>
    Task<WorkflowTransitionResult> ArchiveReportAsync(int reportId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of users who should be notified for review requests.
    /// </summary>
    Task<IEnumerable<User>> GetReviewersForCompanyAsync(int companyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service implementing TTB report workflow state machine.
/// Workflow states: Draft → PendingReview → Approved → Submitted → Archived
/// </summary>
public class TtbReportWorkflowService : ITtbReportWorkflowService
{
    private readonly CaskrDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITtbAuditLogger _auditLogger;
    private readonly IUsersService _usersService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<TtbReportWorkflowService> _logger;

    public TtbReportWorkflowService(
        CaskrDbContext context,
        IEmailService emailService,
        ITtbAuditLogger auditLogger,
        IUsersService usersService,
        IWebhookService webhookService,
        ILogger<TtbReportWorkflowService> logger)
    {
        _context = context;
        _emailService = emailService;
        _auditLogger = auditLogger;
        _usersService = usersService;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<WorkflowTransitionResult> SubmitForReviewAsync(int reportId, int userId, CancellationToken cancellationToken = default)
    {
        var report = await _context.TtbMonthlyReports
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return WorkflowTransitionResult.Failed("Report not found.");
        }

        // Validate current state
        if (report.Status != TtbReportStatus.Draft)
        {
            return WorkflowTransitionResult.Failed($"Report cannot be submitted for review from {report.Status} status. Only Draft reports can be submitted for review.");
        }

        // Validate that report has no validation errors
        if (!string.IsNullOrEmpty(report.ValidationErrors))
        {
            var errors = JsonSerializer.Deserialize<List<string>>(report.ValidationErrors);
            if (errors?.Count > 0)
            {
                return WorkflowTransitionResult.Failed($"Report has {errors.Count} validation error(s) that must be resolved before submission for review.");
            }
        }

        var oldReport = CloneReportForAudit(report);

        // Transition state
        report.Status = TtbReportStatus.PendingReview;
        report.SubmittedForReviewByUserId = userId;
        report.SubmittedForReviewAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await _auditLogger.LogChangeAsync(TtbAuditAction.Update, report, oldReport, userId, report.CompanyId);

        _logger.LogInformation(
            "Report {ReportId} for {Month}/{Year} submitted for review by user {UserId}",
            reportId, report.ReportMonth, report.ReportYear, userId);

        // Send notification to reviewers
        await SendReviewRequestedNotificationAsync(report, userId, cancellationToken);

        return WorkflowTransitionResult.Succeeded(report);
    }

    public async Task<WorkflowTransitionResult> ApproveReportAsync(int reportId, int userId, string? reviewNotes = null, CancellationToken cancellationToken = default)
    {
        var report = await _context.TtbMonthlyReports
            .Include(r => r.Company)
            .Include(r => r.SubmittedForReviewByUser)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return WorkflowTransitionResult.Failed("Report not found.");
        }

        // Validate user role
        var user = await _usersService.GetUserByIdAsync(userId);
        if (user is null)
        {
            return WorkflowTransitionResult.Failed("User not found.");
        }

        if (!IsComplianceManager(user))
        {
            return WorkflowTransitionResult.Failed("Only Compliance Managers can approve reports.");
        }

        // Validate current state
        if (report.Status != TtbReportStatus.PendingReview)
        {
            return WorkflowTransitionResult.Failed($"Report cannot be approved from {report.Status} status. Only PendingReview reports can be approved.");
        }

        var oldReport = CloneReportForAudit(report);

        // Transition state
        report.Status = TtbReportStatus.Approved;
        report.ReviewedByUserId = userId;
        report.ReviewedAt = DateTime.UtcNow;
        report.ApprovedByUserId = userId;
        report.ApprovedAt = DateTime.UtcNow;
        report.ReviewNotes = reviewNotes;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await _auditLogger.LogChangeAsync(TtbAuditAction.Update, report, oldReport, userId, report.CompanyId);

        _logger.LogInformation(
            "Report {ReportId} for {Month}/{Year} approved by user {UserId}",
            reportId, report.ReportMonth, report.ReportYear, userId);

        // Send notification to submitter
        await SendReportApprovedNotificationAsync(report, userId, cancellationToken);

        return WorkflowTransitionResult.Succeeded(report);
    }

    public async Task<WorkflowTransitionResult> RejectReportAsync(int reportId, int userId, string? reviewNotes = null, CancellationToken cancellationToken = default)
    {
        var report = await _context.TtbMonthlyReports
            .Include(r => r.Company)
            .Include(r => r.SubmittedForReviewByUser)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return WorkflowTransitionResult.Failed("Report not found.");
        }

        // Validate user role
        var user = await _usersService.GetUserByIdAsync(userId);
        if (user is null)
        {
            return WorkflowTransitionResult.Failed("User not found.");
        }

        if (!IsComplianceManager(user))
        {
            return WorkflowTransitionResult.Failed("Only Compliance Managers can reject reports.");
        }

        // Validate current state
        if (report.Status != TtbReportStatus.PendingReview)
        {
            return WorkflowTransitionResult.Failed($"Report cannot be rejected from {report.Status} status. Only PendingReview reports can be rejected.");
        }

        var oldReport = CloneReportForAudit(report);

        // Transition state back to Draft
        report.Status = TtbReportStatus.Draft;
        report.ReviewedByUserId = userId;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewNotes = reviewNotes;
        // Clear approval fields
        report.ApprovedByUserId = null;
        report.ApprovedAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await _auditLogger.LogChangeAsync(TtbAuditAction.Update, report, oldReport, userId, report.CompanyId);

        _logger.LogInformation(
            "Report {ReportId} for {Month}/{Year} rejected by user {UserId}",
            reportId, report.ReportMonth, report.ReportYear, userId);

        // Send notification to submitter
        await SendReportRejectedNotificationAsync(report, userId, cancellationToken);

        return WorkflowTransitionResult.Succeeded(report);
    }

    public async Task<WorkflowTransitionResult> SubmitToTtbAsync(int reportId, int userId, string confirmationNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmationNumber))
        {
            return WorkflowTransitionResult.Failed("TTB confirmation number is required.");
        }

        var report = await _context.TtbMonthlyReports
            .Include(r => r.Company)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.SubmittedForReviewByUser)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return WorkflowTransitionResult.Failed("Report not found.");
        }

        // Validate current state
        if (report.Status != TtbReportStatus.Approved)
        {
            return WorkflowTransitionResult.Failed($"Report cannot be submitted to TTB from {report.Status} status. Only Approved reports can be submitted to TTB.");
        }

        // Re-validate that report has no validation errors before final submission
        if (!string.IsNullOrEmpty(report.ValidationErrors))
        {
            var errors = JsonSerializer.Deserialize<List<string>>(report.ValidationErrors);
            if (errors?.Count > 0)
            {
                return WorkflowTransitionResult.Failed($"Report has {errors.Count} validation error(s) that must be resolved before TTB submission.");
            }
        }

        var oldReport = CloneReportForAudit(report);

        // Transition state
        report.Status = TtbReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.TtbConfirmationNumber = confirmationNumber;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await _auditLogger.LogChangeAsync(TtbAuditAction.Update, report, oldReport, userId, report.CompanyId);

        _logger.LogInformation(
            "Report {ReportId} for {Month}/{Year} submitted to TTB with confirmation {ConfirmationNumber} by user {UserId}",
            reportId, report.ReportMonth, report.ReportYear, confirmationNumber, userId);

        // Send confirmation notification
        await SendSubmissionConfirmedNotificationAsync(report, userId, cancellationToken);

        // Trigger webhook for TTB report submission
        await _webhookService.TriggerEventAsync(
            WebhookEventTypes.TtbReportSubmitted,
            report.Id,
            new
            {
                id = report.Id,
                company_id = report.CompanyId,
                report_month = report.ReportMonth,
                report_year = report.ReportYear,
                form_type = report.FormType.ToString(),
                status = report.Status.ToString(),
                ttb_confirmation_number = report.TtbConfirmationNumber,
                submitted_at = report.SubmittedAt
            },
            report.CompanyId);

        return WorkflowTransitionResult.Succeeded(report);
    }

    public async Task<WorkflowTransitionResult> ArchiveReportAsync(int reportId, int userId, CancellationToken cancellationToken = default)
    {
        var report = await _context.TtbMonthlyReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return WorkflowTransitionResult.Failed("Report not found.");
        }

        // Validate current state - can archive from Submitted or Approved
        if (report.Status != TtbReportStatus.Submitted)
        {
            return WorkflowTransitionResult.Failed($"Report cannot be archived from {report.Status} status. Only Submitted reports can be archived.");
        }

        var oldReport = CloneReportForAudit(report);

        // Transition state
        report.Status = TtbReportStatus.Archived;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await _auditLogger.LogChangeAsync(TtbAuditAction.Update, report, oldReport, userId, report.CompanyId);

        _logger.LogInformation(
            "Report {ReportId} for {Month}/{Year} archived by user {UserId}",
            reportId, report.ReportMonth, report.ReportYear, userId);

        return WorkflowTransitionResult.Succeeded(report);
    }

    public async Task<IEnumerable<User>> GetReviewersForCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        // Get all users who can review reports: ComplianceManagers, Admins, and SuperAdmins
        var reviewerRoles = new[]
        {
            (int)UserType.ComplianceManager,
            (int)UserType.Admin,
            (int)UserType.SuperAdmin
        };

        return await _context.Users
            .Where(u => u.CompanyId == companyId && reviewerRoles.Contains(u.UserTypeId))
            .ToListAsync(cancellationToken);
    }

    private static bool IsComplianceManager(User user)
    {
        var userType = (UserType)user.UserTypeId;
        return userType is UserType.ComplianceManager or UserType.Admin or UserType.SuperAdmin;
    }

    private static TtbMonthlyReport CloneReportForAudit(TtbMonthlyReport report)
    {
        return new TtbMonthlyReport
        {
            Id = report.Id,
            CompanyId = report.CompanyId,
            ReportMonth = report.ReportMonth,
            ReportYear = report.ReportYear,
            Status = report.Status,
            FormType = report.FormType,
            GeneratedAt = report.GeneratedAt,
            SubmittedAt = report.SubmittedAt,
            TtbConfirmationNumber = report.TtbConfirmationNumber,
            PdfPath = report.PdfPath,
            ValidationErrors = report.ValidationErrors,
            ValidationWarnings = report.ValidationWarnings,
            CreatedByUserId = report.CreatedByUserId,
            SubmittedForReviewByUserId = report.SubmittedForReviewByUserId,
            SubmittedForReviewAt = report.SubmittedForReviewAt,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAt = report.ReviewedAt,
            ApprovedByUserId = report.ApprovedByUserId,
            ApprovedAt = report.ApprovedAt,
            ReviewNotes = report.ReviewNotes
        };
    }

    private async Task SendReviewRequestedNotificationAsync(TtbMonthlyReport report, int submittedByUserId, CancellationToken cancellationToken)
    {
        try
        {
            var submitter = await _usersService.GetUserByIdAsync(submittedByUserId);
            var reviewers = await GetReviewersForCompanyAsync(report.CompanyId, cancellationToken);

            foreach (var reviewer in reviewers)
            {
                if (string.IsNullOrEmpty(reviewer.Email)) continue;

                var subject = $"TTB Report Review Requested - {report.ReportMonth}/{report.ReportYear}";
                var body = GenerateReviewRequestedEmailBody(report, submitter?.Name ?? "Unknown User");

                await _emailService.SendEmailAsync(reviewer.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send review request notification for report {ReportId}", report.Id);
        }
    }

    private async Task SendReportApprovedNotificationAsync(TtbMonthlyReport report, int approvedByUserId, CancellationToken cancellationToken)
    {
        try
        {
            if (report.SubmittedForReviewByUserId is null) return;

            var submitter = await _usersService.GetUserByIdAsync(report.SubmittedForReviewByUserId.Value);
            if (submitter is null || string.IsNullOrEmpty(submitter.Email)) return;

            var approver = await _usersService.GetUserByIdAsync(approvedByUserId);

            var subject = $"TTB Report Approved - {report.ReportMonth}/{report.ReportYear}";
            var body = GenerateReportApprovedEmailBody(report, approver?.Name ?? "Compliance Manager");

            await _emailService.SendEmailAsync(submitter.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send approval notification for report {ReportId}", report.Id);
        }
    }

    private async Task SendReportRejectedNotificationAsync(TtbMonthlyReport report, int rejectedByUserId, CancellationToken cancellationToken)
    {
        try
        {
            if (report.SubmittedForReviewByUserId is null) return;

            var submitter = await _usersService.GetUserByIdAsync(report.SubmittedForReviewByUserId.Value);
            if (submitter is null || string.IsNullOrEmpty(submitter.Email)) return;

            var reviewer = await _usersService.GetUserByIdAsync(rejectedByUserId);

            var subject = $"TTB Report Requires Revision - {report.ReportMonth}/{report.ReportYear}";
            var body = GenerateReportRejectedEmailBody(report, reviewer?.Name ?? "Compliance Manager");

            await _emailService.SendEmailAsync(submitter.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send rejection notification for report {ReportId}", report.Id);
        }
    }

    private async Task SendSubmissionConfirmedNotificationAsync(TtbMonthlyReport report, int submittedByUserId, CancellationToken cancellationToken)
    {
        try
        {
            // Notify all relevant parties: submitter, approver, and TTB contacts
            var notifyUserIds = new HashSet<int>();

            if (report.SubmittedForReviewByUserId.HasValue)
                notifyUserIds.Add(report.SubmittedForReviewByUserId.Value);

            if (report.ApprovedByUserId.HasValue)
                notifyUserIds.Add(report.ApprovedByUserId.Value);

            // Also notify TTB contacts for the company
            var ttbContacts = await _context.Users
                .Where(u => u.CompanyId == report.CompanyId && u.IsTtbContact)
                .ToListAsync(cancellationToken);

            foreach (var contact in ttbContacts)
            {
                notifyUserIds.Add(contact.Id);
            }

            foreach (var userId in notifyUserIds)
            {
                var user = await _usersService.GetUserByIdAsync(userId);
                if (user is null || string.IsNullOrEmpty(user.Email)) continue;

                var subject = $"TTB Report Submitted - Confirmation #{report.TtbConfirmationNumber}";
                var body = GenerateSubmissionConfirmedEmailBody(report);

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send submission confirmation notification for report {ReportId}", report.Id);
        }
    }

    private static string GenerateReviewRequestedEmailBody(TtbMonthlyReport report, string submitterName)
    {
        return $@"
TTB Report Review Requested

A TTB monthly report has been submitted for your review.

Report Details:
- Company: {report.Company?.CompanyName ?? "Unknown"}
- Period: {report.ReportMonth}/{report.ReportYear}
- Form Type: {report.FormType}
- Submitted By: {submitterName}
- Submitted At: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC

Please review this report and either approve it for TTB submission or reject it with feedback.

This is an automated notification from Caskr.
";
    }

    private static string GenerateReportApprovedEmailBody(TtbMonthlyReport report, string approverName)
    {
        var reviewNotes = !string.IsNullOrEmpty(report.ReviewNotes)
            ? $"\nReviewer Notes: {report.ReviewNotes}"
            : "";

        return $@"
TTB Report Approved

Your TTB monthly report has been approved and is ready for submission to TTB.

Report Details:
- Company: {report.Company?.CompanyName ?? "Unknown"}
- Period: {report.ReportMonth}/{report.ReportYear}
- Form Type: {report.FormType}
- Approved By: {approverName}
- Approved At: {report.ApprovedAt:yyyy-MM-dd HH:mm} UTC
{reviewNotes}

You can now submit this report to TTB and record the confirmation number in Caskr.

This is an automated notification from Caskr.
";
    }

    private static string GenerateReportRejectedEmailBody(TtbMonthlyReport report, string reviewerName)
    {
        var reviewNotes = !string.IsNullOrEmpty(report.ReviewNotes)
            ? $"\nReviewer Notes: {report.ReviewNotes}"
            : "\nNo specific feedback was provided.";

        return $@"
TTB Report Requires Revision

Your TTB monthly report has been reviewed and requires changes before it can be approved.

Report Details:
- Company: {report.Company?.CompanyName ?? "Unknown"}
- Period: {report.ReportMonth}/{report.ReportYear}
- Form Type: {report.FormType}
- Reviewed By: {reviewerName}
- Reviewed At: {report.ReviewedAt:yyyy-MM-dd HH:mm} UTC
{reviewNotes}

Please address the feedback and resubmit the report for review.

This is an automated notification from Caskr.
";
    }

    private static string GenerateSubmissionConfirmedEmailBody(TtbMonthlyReport report)
    {
        return $@"
TTB Report Submission Confirmed

A TTB monthly report has been successfully submitted to the TTB.

Report Details:
- Company: {report.Company?.CompanyName ?? "Unknown"}
- Period: {report.ReportMonth}/{report.ReportYear}
- Form Type: {report.FormType}
- TTB Confirmation Number: {report.TtbConfirmationNumber}
- Submitted At: {report.SubmittedAt:yyyy-MM-dd HH:mm} UTC

This report is now locked and cannot be modified. All related transaction data for this period is also locked for compliance purposes.

This is an automated notification from Caskr.
";
    }
}
