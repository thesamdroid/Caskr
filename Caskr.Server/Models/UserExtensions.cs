using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public partial class User
{
    [MaxLength(100)]
    public string? KeycloakUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}

public partial class Company
{
    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(50)]
    public string? TtbPermitNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

public partial class UserType
{
    [MaxLength(500)]
    public string? Description { get; set; }
}
