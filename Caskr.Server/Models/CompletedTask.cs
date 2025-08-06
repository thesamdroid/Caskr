namespace Caskr.server.Models;

public partial class CompletedTask
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
