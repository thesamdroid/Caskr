namespace Caskr.server.Models;

public partial class Barrel
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Sku { get; set; } = null!;

    public int BatchId { get; set; }

    public int OrderId { get; set; }

    public int RickhouseId { get; set; }

    public int WarehouseId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Rickhouse Rickhouse { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual Batch? Batch { get; set; }

    public virtual ICollection<BarrelTransfer> BarrelTransfers { get; set; } = new List<BarrelTransfer>();
}
