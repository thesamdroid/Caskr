using System;
using System.IO;
using Xunit;

namespace Caskr.Server.Tests;

public class DatabaseMigrationTests
{
    [Fact]
    public void QuickBooksMigrationScript_DefinesExpectedTables()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "07-migration-accounting-integrations.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("CREATE TABLE IF NOT EXISTS public.accounting_integrations", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.accounting_sync_logs", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.chart_of_accounts_mapping", scriptContents);
    }

    [Fact]
    public void TtbComplianceMigrationScript_DefinesExpectedArtifacts()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "07-migration-ttb-compliance.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_monthly_reports", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_monthly_reports_company_month_year", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_inventory_snapshots", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_inventory_snapshots_company_date", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_transactions", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_transactions_company_date", scriptContents);
    }
}
