using System.Collections.Generic;
using System.Threading.Tasks;
using Caskr.Server.Models;

namespace Caskr.Server.Services;

/// <summary>
///     Provides authenticated access to QuickBooks Online data for a connected company.
/// </summary>
public interface IQuickBooksDataService
{
    /// <summary>
    ///     Retrieves the active QuickBooks chart of accounts for the specified company. Implementations must ensure the
    ///     access token is refreshed when necessary, minimize API calls via caching, and surface descriptive errors when
    ///     QuickBooks responds with a failure.
    /// </summary>
    /// <param name="companyId">The company whose chart of accounts should be loaded.</param>
    /// <returns>A list of QuickBooks accounts available to the company.</returns>
    Task<List<QBOAccount>> GetChartOfAccountsAsync(int companyId);
}
