using System;
using System.Collections.Generic;

namespace Caskr.Server.Models;

public partial class UserType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
