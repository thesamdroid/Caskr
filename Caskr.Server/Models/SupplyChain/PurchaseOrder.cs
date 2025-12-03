using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models.SupplyChain;

public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    Confirmed,
    Partial_Received,
    Received,
    Cancelled
}

public enum PaymentStatus
{
    Unpaid,
    Partial,
    Paid
}

[Table("purchase_orders")]
public class PurchaseOrder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("po_number")]
    [MaxLength(100)]
    public string PoNumber { get; set; } = string.Empty;

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("expected_delivery_date")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Column("status")]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [Column("total_amount")]
    public decimal? TotalAmount { get; set; }

    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by_user_id")]
    public int? CreatedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    public virtual ICollection<InventoryReceipt> Receipts { get; set; } = new List<InventoryReceipt>();
}
