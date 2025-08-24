namespace Caskr.server.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int UserTypeId { get; set; }

    public int CompanyId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string CompanyName { get; set; } = string.Empty;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Company Company { get; set; } = null!;

    public virtual UserType UserType { get; set; } = null!;
}
