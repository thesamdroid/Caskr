namespace Caskr.server.Models;

public partial class Company
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public int PrimaryContactId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime RenewalDate { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Rickhouse> Rickhouses { get; set; } = new List<Rickhouse>();

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();

    public virtual User PrimaryContact { get; set; } = null!;
}
