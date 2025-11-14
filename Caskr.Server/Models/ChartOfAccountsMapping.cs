using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class ChartOfAccountsMapping
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public CaskrAccountType CaskrAccountType { get; set; }

    [Required]
    [MaxLength(128)]
    public string QboAccountId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? QboAccountName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;
}
