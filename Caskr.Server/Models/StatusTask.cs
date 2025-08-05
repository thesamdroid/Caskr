namespace Caskr.server.Models;

public partial class StatusTask
{
    public int Id { get; set; }

    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;
}
