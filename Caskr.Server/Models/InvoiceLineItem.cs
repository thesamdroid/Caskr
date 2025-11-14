using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

public class InvoiceLineItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsTaxable { get; set; }

    public CaskrAccountType AccountType { get; set; }

    [MaxLength(64)]
    public string? ProductCode { get; set; }

    [MaxLength(256)]
    public string? ProductName { get; set; }

    public decimal? DiscountAmount { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
