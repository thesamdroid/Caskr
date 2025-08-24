namespace Caskr.server.Models;

public partial class MashBill
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public int[] ComponentIds { get; set; } = System.Array.Empty<int>();
}

