namespace Caskr.server.Models;

public partial class Rickhouse
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();
}
