using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

[Table("purchase_order_items")]
public class PurchaseOrderItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("purchase_order_id")]
    public int PurchaseOrderId { get; set; }

    [Column("supplier_product_id")]
    public int SupplierProductId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("received_quantity")]
    public decimal ReceivedQuantity { get; set; } = 0;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("SupplierProductId")]
    public virtual SupplierProduct? SupplierProduct { get; set; }

    public virtual ICollection<InventoryReceiptItem> ReceiptItems { get; set; } = new List<InventoryReceiptItem>();
}
