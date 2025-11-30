namespace Caskr.server.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int UserTypeId { get; set; }

    public int CompanyId { get; set; }

    public bool IsPrimaryContact { get; set; }

    public bool IsTtbContact { get; set; }

    public bool IsActive { get; set; } = true;

    public string? KeycloakUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string CompanyName { get; set; } = string.Empty;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<TtbMonthlyReport> CreatedTtbMonthlyReports { get; set; } = new List<TtbMonthlyReport>();

    public virtual Company Company { get; set; } = null!;

    public virtual UserType UserType { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string TemporaryPassword { get; set; } = string.Empty;
}
