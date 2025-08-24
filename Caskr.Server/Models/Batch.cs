namespace Caskr.server.Models;

public partial class Batch
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int MashBillId { get; set; }

    public virtual MashBill MashBill { get; set; } = null!;
}

