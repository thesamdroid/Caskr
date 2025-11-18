using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class TtbInventorySnapshot
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public DateTime SnapshotDate { get; set; }

    [Required]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    public TtbSpiritsType SpiritsType { get; set; }

    public decimal ProofGallons { get; set; }

    public decimal WineGallons { get; set; }

    [Required]
    public TtbTaxStatus TaxStatus { get; set; }

    public virtual Company Company { get; set; } = null!;
}
