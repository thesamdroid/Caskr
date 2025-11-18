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
    public void AccountingSyncPreferencesMigration_DefinesTable()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "08-migration-accounting-sync-preferences.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("CREATE TABLE IF NOT EXISTS public.accounting_sync_preferences", scriptContents);
        Assert.Contains("ux_accounting_sync_preferences_company_provider", scriptContents);
    }
}
