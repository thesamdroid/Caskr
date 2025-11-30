using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class TtbMonthlyReport
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [Range(1, 12)]
    public int ReportMonth { get; set; }

    [Required]
    public int ReportYear { get; set; }

    [Required]
    public TtbReportStatus Status { get; set; }

    [Required]
    public TtbFormType FormType { get; set; } = TtbFormType.Form5110_28;

    public DateTime? GeneratedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? TtbConfirmationNumber { get; set; }

    public string? PdfPath { get; set; }

    public string? ValidationErrors { get; set; }

    public string? ValidationWarnings { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// User who submitted the report for review (Draft → PendingReview transition).
    /// </summary>
    public int? SubmittedForReviewByUserId { get; set; }

    /// <summary>
    /// Timestamp when the report was submitted for review.
    /// </summary>
    public DateTime? SubmittedForReviewAt { get; set; }

    /// <summary>
    /// User who reviewed the report (typically a Compliance Manager).
    /// </summary>
    public int? ReviewedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the report was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// User who approved the report for TTB submission.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the report was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Notes or comments from the reviewer during review/approval.
    /// </summary>
    public string? ReviewNotes { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual User? SubmittedForReviewByUser { get; set; }

    public virtual User? ReviewedByUser { get; set; }

    public virtual User? ApprovedByUser { get; set; }
}
