namespace Caskr.server.Models;

using System;

public partial class TaskItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime CompletedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
