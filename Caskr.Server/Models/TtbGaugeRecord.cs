using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class TtbGaugeRecord
{
    public int Id { get; set; }

    [Required]
    public int BarrelId { get; set; }

    [Required]
    public DateTime GaugeDate { get; set; }

    [Required]
    public TtbGaugeType GaugeType { get; set; }

    [Required]
    public decimal Proof { get; set; }

    [Required]
    public decimal Temperature { get; set; }

    [Required]
    public decimal WineGallons { get; set; }

    public decimal ProofGallons { get; set; }

    public int? GaugedByUserId { get; set; }

    public string? Notes { get; set; }

    public virtual Barrel Barrel { get; set; } = null!;

    public virtual User? GaugedByUser { get; set; }
}
