namespace Caskr.server.Models;

using System;
using Caskr.server.Models.Crm;

public partial class Order
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int OwnerId { get; set; }

    public int CompanyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int StatusId { get; set; }

    public int SpiritTypeId { get; set; }

    public int BatchId { get; set; }

    public int Quantity { get; set; }

    public int? InvoiceId { get; set; }

    /// <summary>
    /// Warehouse that will fulfill this order (source of barrels)
    /// </summary>
    public int? FulfillmentWarehouseId { get; set; }

    // Salesforce CRM integration fields (CRM-001)
    /// <summary>
    /// Reference to the customer record (for CRM integration)
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// Salesforce Opportunity ID (18 character format)
    /// </summary>
    public string? SalesforceOpportunityId { get; set; }

    /// <summary>
    /// Timestamp of last Salesforce sync
    /// </summary>
    public DateTime? SalesforceLastSyncAt { get; set; }

    /// <summary>
    /// Order date from Salesforce Opportunity CloseDate
    /// </summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>
    /// Total amount from Salesforce Opportunity Amount
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Order notes from Salesforce Opportunity Name/Description
    /// </summary>
    public string? OrderNotes { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int MashBillId { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Warehouse? FulfillmentWarehouse { get; set; }

    public virtual Batch? Batch { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual SpiritType SpiritType { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    /// <summary>
    /// Customer associated with this order (for CRM integration)
    /// </summary>
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderTask> Tasks { get; set; } = new List<OrderTask>();

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();
}
