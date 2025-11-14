using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

public class Invoice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [MaxLength(64)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [MaxLength(256)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? CustomerEmail { get; set; }

    [MaxLength(64)]
    public string? CustomerPhone { get; set; }

    [MaxLength(256)]
    public string? CustomerAddressLine1 { get; set; }

    [MaxLength(256)]
    public string? CustomerAddressLine2 { get; set; }

    [MaxLength(128)]
    public string? CustomerCity { get; set; }

    [MaxLength(128)]
    public string? CustomerState { get; set; }

    [MaxLength(32)]
    public string? CustomerPostalCode { get; set; }

    [MaxLength(128)]
    public string? CustomerCountry { get; set; }

    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(1024)]
    public string? Notes { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

    public virtual ICollection<InvoiceTax> Taxes { get; set; } = new List<InvoiceTax>();
}
