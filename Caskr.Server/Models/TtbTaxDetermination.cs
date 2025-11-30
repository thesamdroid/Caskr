using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

/// <summary>
/// Represents a federal excise tax determination for removed spirits
/// </summary>
public class TtbTaxDetermination
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public decimal ProofGallons { get; set; }

    [Required]
    public decimal TaxRate { get; set; }

    [Required]
    public decimal TaxAmount { get; set; }

    [Required]
    public DateTime DeterminationDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public string? PaymentReference { get; set; }

    public string? QuickBooksJournalEntryId { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
