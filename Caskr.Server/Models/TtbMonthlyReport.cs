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

    public DateTime? GeneratedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? TtbConfirmationNumber { get; set; }

    public string? PdfPath { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
}
