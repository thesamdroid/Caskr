using System.Collections.Generic;
using System.Linq;
using Caskr.server;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;

namespace Caskr.Server.Services;

/// <summary>
///     Executes QuickBooks SDK queries for account data.
/// </summary>
public interface IQuickBooksAccountQueryClient
{
    /// <summary>
    ///     Executes the standard chart of accounts query using the provided service context.
    /// </summary>
    IReadOnlyList<Account> ExecuteActiveAccountQuery(ServiceContext serviceContext);
}

[AutoBind]
public sealed class QuickBooksAccountQueryClient : IQuickBooksAccountQueryClient
{
    private const string ActiveAccountsQuery = "SELECT * FROM Account WHERE Active = true";

    public IReadOnlyList<Account> ExecuteActiveAccountQuery(ServiceContext serviceContext)
    {
        var queryService = new QueryService<Account>(serviceContext);
        var accounts = queryService.ExecuteIdsQuery(ActiveAccountsQuery);
        return accounts?.ToList() ?? new List<Account>();
    }
}
