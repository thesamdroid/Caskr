using System.Threading;
using System.Threading.Tasks;
using Intuit.Ipp.Core;
using IppCustomer = Intuit.Ipp.Data.Customer;
using QuickBooksInvoice = Intuit.Ipp.Data.Invoice;

namespace Caskr.Server.Services;

/// <summary>
///     Wraps the Intuit DataService calls that are required to synchronize invoices and customers.
///     Separating the SDK usage behind this abstraction keeps <see cref="QuickBooksInvoiceSyncService" /> testable while
///     still relying on the official QuickBooks SDK in production.
/// </summary>
public interface IQuickBooksInvoiceClient
{
    Task<IppCustomer?> FindCustomerByEmailAsync(ServiceContext context, string email, CancellationToken cancellationToken);

    Task<IppCustomer?> FindCustomerByDisplayNameAsync(ServiceContext context, string displayName, CancellationToken cancellationToken);

    Task<IppCustomer> CreateCustomerAsync(ServiceContext context, IppCustomer customer, CancellationToken cancellationToken);

    Task<QuickBooksInvoice> CreateInvoiceAsync(ServiceContext context, QuickBooksInvoice invoice, CancellationToken cancellationToken);
}
