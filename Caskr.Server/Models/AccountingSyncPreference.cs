using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models;

public class AccountingSyncPreference
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public AccountingProvider Provider { get; set; }

    [Required]
    public bool AutoSyncInvoices { get; set; }

    [Required]
    public bool AutoSyncCogs { get; set; }

    [Required]
    [MaxLength(32)]
    public string SyncFrequency { get; set; } = "Manual";

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company Company { get; set; } = null!;
}
