using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class AccountingIntegration
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public AccountingProvider Provider { get; set; }

    [MaxLength(4096)]
    public string? AccessTokenEncrypted { get; set; }

    [MaxLength(4096)]
    public string? RefreshTokenEncrypted { get; set; }

    [MaxLength(255)]
    public string? RealmId { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;
}
