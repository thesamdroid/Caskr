using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using Microsoft.Extensions.Logging;
using IppCustomer = Intuit.Ipp.Data.Customer;
using QuickBooksInvoice = Intuit.Ipp.Data.Invoice;

namespace Caskr.Server.Services;

/// <summary>
///     Default implementation of <see cref="IQuickBooksInvoiceClient" /> that uses the Intuit SDK <see cref="DataService" />
///     to query customers and create invoices.
/// </summary>
[AutoBind]
public class QuickBooksInvoiceClient : IQuickBooksInvoiceClient
{
    private readonly ILogger<QuickBooksInvoiceClient> _logger;

    public QuickBooksInvoiceClient(ILogger<QuickBooksInvoiceClient> logger)
    {
        _logger = logger;
    }

    public Task<IppCustomer?> FindCustomerByEmailAsync(ServiceContext context, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<IppCustomer?>(null);
        }

        var query = $"select * from Customer where PrimaryEmailAddr = '{EscapeForQuery(email)}'";
        return ExecuteQueryAsync(context, query, cancellationToken);
    }

    public Task<IppCustomer?> FindCustomerByDisplayNameAsync(ServiceContext context, string displayName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Task.FromResult<IppCustomer?>(null);
        }

        var query = $"select * from Customer where DisplayName = '{EscapeForQuery(displayName)}'";
        return ExecuteQueryAsync(context, query, cancellationToken);
    }

    public Task<IppCustomer> CreateCustomerAsync(ServiceContext context, IppCustomer customer, CancellationToken cancellationToken)
    {
        if (customer is null)
        {
            throw new ArgumentNullException(nameof(customer));
        }

        return ExecuteAsync(context, ds =>
        {
            _logger.LogInformation("Creating customer {DisplayName} in QuickBooks", customer.DisplayName);
            return ds.Add(customer);
        }, cancellationToken);
    }

    public Task<QuickBooksInvoice> CreateInvoiceAsync(ServiceContext context, QuickBooksInvoice invoice, CancellationToken cancellationToken)
    {
        if (invoice is null)
        {
            throw new ArgumentNullException(nameof(invoice));
        }

        return ExecuteAsync(context, ds =>
        {
            _logger.LogInformation("Creating QuickBooks invoice for customer {CustomerRef}", invoice.CustomerRef?.Value);
            return ds.Add(invoice);
        }, cancellationToken);
    }

    private Task<IppCustomer?> ExecuteQueryAsync(ServiceContext context, string query, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, ds =>
        {
            _logger.LogDebug("Executing QuickBooks query: {Query}", query);
            var queryService = new QueryService<IppCustomer>(context);
            var entities = queryService.ExecuteIdsQuery(query);
            return entities?.FirstOrDefault();
        }, cancellationToken);
    }

    private static Task<T> ExecuteAsync<T>(ServiceContext context, Func<DataService, T> action, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return Task.Run(() =>
        {
            var dataService = new DataService(context);
            return action(dataService);
        }, cancellationToken);
    }

    private static string EscapeForQuery(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
