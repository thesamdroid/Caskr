using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class AccountingSyncLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [MaxLength(128)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? EntityId { get; set; }

    [MaxLength(256)]
    public string? ExternalEntityId { get; set; }

    [Required]
    public SyncStatus SyncStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    [Required]
    public DateTime SyncedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;
}
