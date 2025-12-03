using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

[Table("inventory_receipts")]
public class InventoryReceipt
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("purchase_order_id")]
    public int PurchaseOrderId { get; set; }

    [Column("receipt_date")]
    public DateTime ReceiptDate { get; set; }

    [Column("received_by_user_id")]
    public int? ReceivedByUserId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("ReceivedByUserId")]
    public virtual User? ReceivedByUser { get; set; }

    public virtual ICollection<InventoryReceiptItem> Items { get; set; } = new List<InventoryReceiptItem>();
}
