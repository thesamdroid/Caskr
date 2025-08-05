namespace Caskr.server.Models;

public partial class Status
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<StatusTask> StatusTasks { get; set; } = new List<StatusTask>();
}
