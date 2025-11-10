using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class TransferRequest
{
    [Required]
    public int FromCompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ToCompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PermitNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Barrel count must be at least 1")]
    public int BarrelCount { get; set; }

    /// <summary>
    /// Optional: If provided, barrel details will be fetched and included in the form
    /// </summary>
    public int? OrderId { get; set; }
}
