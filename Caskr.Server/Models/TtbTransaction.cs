using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class TtbTransaction
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    [Required]
    public TtbTransactionType TransactionType { get; set; }

    [Required]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    public TtbSpiritsType SpiritsType { get; set; }

    public decimal ProofGallons { get; set; }

    public decimal WineGallons { get; set; }

    public string? SourceEntityType { get; set; }

    public int? SourceEntityId { get; set; }

    public string? Notes { get; set; }

    public virtual Company Company { get; set; } = null!;
}
