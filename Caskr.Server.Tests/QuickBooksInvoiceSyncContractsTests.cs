using System.Linq;
using System.Threading.Tasks;
using Caskr.Server.Services;
using Xunit;

namespace Caskr.Server.Tests;

public class QuickBooksInvoiceSyncContractsTests
{
    [Fact]
    public void SyncInvoiceToQboAsync_ReturnsInvoiceSyncResult()
    {
        var method = typeof(IQuickBooksInvoiceSyncService).GetMethod(nameof(IQuickBooksInvoiceSyncService.SyncInvoiceToQBOAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<InvoiceSyncResult>), method!.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(int), method.GetParameters().Single().ParameterType);
    }

    [Fact]
    public void SyncPaymentToQboAsync_ReturnsPaymentSyncResult()
    {
        var method = typeof(IQuickBooksInvoiceSyncService).GetMethod(nameof(IQuickBooksInvoiceSyncService.SyncPaymentToQBOAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<PaymentSyncResult>), method!.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(int), method.GetParameters().Single().ParameterType);
    }

    [Fact]
    public void BatchSyncResult_DefaultsToEmptyErrors()
    {
        var result = new BatchSyncResult(1, 0);

        Assert.Empty(result.Errors);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public void InvoiceSyncResult_PreservesSuppliedValues()
    {
        var result = new InvoiceSyncResult(true, "123", null);

        Assert.True(result.Success);
        Assert.Equal("123", result.QboInvoiceId);
        Assert.Null(result.ErrorMessage);
    }
}
