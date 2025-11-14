using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

public class InvoiceTax
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [MaxLength(128)]
    public string TaxName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string TaxCode { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
