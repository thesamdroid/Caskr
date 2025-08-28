namespace Caskr.server.Models;

public class TransferRequest
{
    public int FromCompanyId { get; set; }
    public string ToCompanyName { get; set; } = string.Empty;
    public string PermitNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int BarrelCount { get; set; }
}
