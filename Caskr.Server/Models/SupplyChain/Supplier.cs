using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

public enum SupplierType
{
    Grain,
    Cooperage,
    Bottles,
    Labels,
    Chemicals,
    Equipment,
    Other
}

[Table("suppliers")]
public class Supplier
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("supplier_name")]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Column("supplier_type")]
    public SupplierType SupplierType { get; set; } = SupplierType.Other;

    [Column("contact_person")]
    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Column("phone")]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("website")]
    [MaxLength(500)]
    public string? Website { get; set; }

    [Column("payment_terms")]
    [MaxLength(100)]
    public string? PaymentTerms { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    public virtual ICollection<SupplierProduct> Products { get; set; } = new List<SupplierProduct>();

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
