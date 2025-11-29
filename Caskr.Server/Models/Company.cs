using System;
using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public partial class Company
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime RenewalDate { get; set; }

    public bool AutoGenerateTtbReports { get; set; }

    [Required]
    public TtbAutoReportCadence TtbAutoReportCadence { get; set; } = TtbAutoReportCadence.Monthly;

    [Range(0, 23)]
    public int TtbAutoReportHourUtc { get; set; } = 6;

    [Range(1, 28)]
    public int TtbAutoReportDayOfMonth { get; set; } = 1;

    public DayOfWeek TtbAutoReportDayOfWeek { get; set; } = DayOfWeek.Monday;

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Rickhouse> Rickhouses { get; set; } = new List<Rickhouse>();

    public virtual ICollection<Barrel> Barrels { get; set; } = new List<Barrel>();

    public virtual ICollection<AccountingIntegration> AccountingIntegrations { get; set; } = new List<AccountingIntegration>();

    public virtual ICollection<AccountingSyncLog> AccountingSyncLogs { get; set; } = new List<AccountingSyncLog>();

    public virtual ICollection<ChartOfAccountsMapping> ChartOfAccountsMappings { get; set; } = new List<ChartOfAccountsMapping>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<TtbMonthlyReport> TtbMonthlyReports { get; set; } = new List<TtbMonthlyReport>();

    public virtual ICollection<TtbInventorySnapshot> TtbInventorySnapshots { get; set; } = new List<TtbInventorySnapshot>();

    public virtual ICollection<TtbTransaction> TtbTransactions { get; set; } = new List<TtbTransaction>();
}
