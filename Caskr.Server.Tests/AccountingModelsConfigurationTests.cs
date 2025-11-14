using System;
using System.Linq;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Caskr.Server.Tests;

public class AccountingModelsConfigurationTests
{
    [Fact]
    public void CaskrDbContext_ConfiguresAccountingEntitiesWithExpectedSchema()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);

        var accountingIntegration = context.Model.FindEntityType(typeof(AccountingIntegration));
        Assert.NotNull(accountingIntegration);
        Assert.Equal("accounting_integrations", accountingIntegration!.GetTableName());
        var providerColumn = accountingIntegration.FindProperty(nameof(AccountingIntegration.Provider));
        Assert.Equal(
            "provider",
            providerColumn!.GetColumnName(StoreObjectIdentifier.Table("accounting_integrations", null)));
        var accountingIntegrationFk = accountingIntegration.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(AccountingIntegration.CompanyId)));
        Assert.Equal(typeof(Company), accountingIntegrationFk.PrincipalEntityType.ClrType);

        var accountingSyncLog = context.Model.FindEntityType(typeof(AccountingSyncLog));
        Assert.NotNull(accountingSyncLog);
        Assert.Equal("accounting_sync_logs", accountingSyncLog!.GetTableName());
        var syncStatusColumn = accountingSyncLog.FindProperty(nameof(AccountingSyncLog.SyncStatus));
        Assert.Equal(
            "sync_status",
            syncStatusColumn!.GetColumnName(StoreObjectIdentifier.Table("accounting_sync_logs", null)));
        var accountingSyncLogFk = accountingSyncLog.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(AccountingSyncLog.CompanyId)));
        Assert.Equal(typeof(Company), accountingSyncLogFk.PrincipalEntityType.ClrType);

        var chartOfAccountsMapping = context.Model.FindEntityType(typeof(ChartOfAccountsMapping));
        Assert.NotNull(chartOfAccountsMapping);
        Assert.Equal("chart_of_accounts_mapping", chartOfAccountsMapping!.GetTableName());
        var caskrAccountTypeColumn = chartOfAccountsMapping.FindProperty(nameof(ChartOfAccountsMapping.CaskrAccountType));
        Assert.Equal(
            "caskr_account_type",
            caskrAccountTypeColumn!.GetColumnName(StoreObjectIdentifier.Table("chart_of_accounts_mapping", null)));
        var chartOfAccountsFk = chartOfAccountsMapping.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(ChartOfAccountsMapping.CompanyId)));
        Assert.Equal(typeof(Company), chartOfAccountsFk.PrincipalEntityType.ClrType);
    }
}
