namespace Caskr.server.Models;

using System;

public partial class Product
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Owner { get; set; } = null!;
}
