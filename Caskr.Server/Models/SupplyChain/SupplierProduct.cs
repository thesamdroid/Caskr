using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

[Table("supplier_products")]
public class SupplierProduct
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("product_name")]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Column("product_category")]
    [MaxLength(100)]
    public string? ProductCategory { get; set; }

    [Column("sku")]
    [MaxLength(100)]
    public string? Sku { get; set; }

    [Column("unit_of_measure")]
    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }

    [Column("current_price")]
    public decimal? CurrentPrice { get; set; }

    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Column("lead_time_days")]
    public int? LeadTimeDays { get; set; }

    [Column("minimum_order_quantity")]
    public int? MinimumOrderQuantity { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
