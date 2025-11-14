namespace Caskr.server.Models;

public partial class Company
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime RenewalDate { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Rickhouse> Rickhouses { get; set; } = new List<Rickhouse>();

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();

    public virtual ICollection<AccountingIntegration> AccountingIntegrations { get; set; } = new List<AccountingIntegration>();

    public virtual ICollection<AccountingSyncLog> AccountingSyncLogs { get; set; } = new List<AccountingSyncLog>();

    public virtual ICollection<ChartOfAccountsMapping> ChartOfAccountsMappings { get; set; } = new List<ChartOfAccountsMapping>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
