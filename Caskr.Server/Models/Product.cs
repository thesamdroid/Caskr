using System;
using System.Collections.Generic;

namespace Caskr.Server.Models;

public partial class Product
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? Notes { get; set; }

    public virtual User Owner { get; set; } = null!;
}
