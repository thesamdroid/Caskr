namespace Caskr.server.Models;

using System;

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

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int MashBillId { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Batch? Batch { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual SpiritType SpiritType { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    public virtual ICollection<OrderTask> Tasks { get; set; } = new List<OrderTask>();

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();
}
