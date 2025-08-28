namespace Caskr.server.Models;

public class LabelRequest
{
    public int CompanyId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string AlcoholContent { get; set; } = string.Empty;
}
