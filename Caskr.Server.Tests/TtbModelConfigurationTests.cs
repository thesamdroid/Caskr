using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Caskr.Server.Tests;

public class TtbModelConfigurationTests
{
    [Fact]
    public void DbContext_ShouldExposeTtbEntitiesWithSnakeCaseTables()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new CaskrDbContext(options);

        var reportEntity = context.Model.FindEntityType(typeof(TtbMonthlyReport));
        var snapshotEntity = context.Model.FindEntityType(typeof(TtbInventorySnapshot));
        var transactionEntity = context.Model.FindEntityType(typeof(TtbTransaction));

        Assert.NotNull(reportEntity);
        Assert.Equal("ttb_monthly_reports", reportEntity!.GetTableName());

        Assert.NotNull(snapshotEntity);
        Assert.Equal("ttb_inventory_snapshots", snapshotEntity!.GetTableName());

        Assert.NotNull(transactionEntity);
        Assert.Equal("ttb_transactions", transactionEntity!.GetTableName());
    }
}
