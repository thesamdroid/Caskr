using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class LabelRequest
{
    [Required]
    public int CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string BrandName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AlcoholContent { get; set; } = string.Empty;
}
