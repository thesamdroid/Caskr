namespace Caskr.server.Models;

public partial class Order
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int OwnerId { get; set; }

    public DateOnly CreatedDate { get; set; }

    public StatusType StatusId { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;
}
