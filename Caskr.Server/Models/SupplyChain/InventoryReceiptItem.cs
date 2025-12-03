using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

public enum ReceiptItemCondition
{
    Good,
    Damaged,
    Partial
}

[Table("inventory_receipt_items")]
public class InventoryReceiptItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("inventory_receipt_id")]
    public int InventoryReceiptId { get; set; }

    [Column("purchase_order_item_id")]
    public int PurchaseOrderItemId { get; set; }

    [Column("received_quantity")]
    public decimal ReceivedQuantity { get; set; }

    [Column("condition")]
    public ReceiptItemCondition Condition { get; set; } = ReceiptItemCondition.Good;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("InventoryReceiptId")]
    public virtual InventoryReceipt? InventoryReceipt { get; set; }

    [ForeignKey("PurchaseOrderItemId")]
    public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }
}
