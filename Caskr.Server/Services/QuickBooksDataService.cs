using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caskr.Server.Models;
using Intuit.Ipp.Data;
using Intuit.Ipp.Exception;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Provides data access helpers for QuickBooks Online APIs.
/// </summary>
public class QuickBooksDataService : IQuickBooksDataService
{
    private readonly IMemoryCache _cache;
    private readonly IQuickBooksIntegrationContextFactory _contextFactory;
    private readonly IQuickBooksAccountQueryClient _accountQueryClient;
    private readonly ILogger<QuickBooksDataService> _logger;

    public QuickBooksDataService(
        IMemoryCache cache,
        IQuickBooksIntegrationContextFactory contextFactory,
        IQuickBooksAccountQueryClient accountQueryClient,
        ILogger<QuickBooksDataService> logger)
    {
        _cache = cache;
        _contextFactory = contextFactory;
        _accountQueryClient = accountQueryClient;
        _logger = logger;
    }

    public async Task<List<QBOAccount>> GetChartOfAccountsAsync(int companyId, bool bypassCache = false)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId));
        }

        var cacheKey = GetCacheKey(companyId);
        if (bypassCache)
        {
            _cache.Remove(cacheKey);
        }
        else if (_cache.TryGetValue(cacheKey, out List<QBOAccount>? cachedAccounts) && cachedAccounts is not null)
        {
            _logger.LogInformation("Returning cached QuickBooks accounts for company {CompanyId}", companyId);
            return cachedAccounts;
        }

        var integrationContext = await _contextFactory.CreateAsync(companyId);
        if (integrationContext is null)
        {
            throw new InvalidOperationException($"QuickBooks integration context could not be created for company {companyId}.");
        }
        var serviceContext = integrationContext.ServiceContext;
        var realmId = integrationContext.RealmId;

        try
        {
            _logger.LogInformation("Fetching QuickBooks chart of accounts for company {CompanyId} (realm {RealmId})", companyId, realmId);
            var qbAccounts = _accountQueryClient.ExecuteActiveAccountQuery(serviceContext);
            var mappedAccounts = qbAccounts.Select(MapAccount).ToList();
            _cache.Set(cacheKey, mappedAccounts, QuickBooksConstants.CacheConfiguration.ChartOfAccountsCacheDuration);
            return mappedAccounts;
        }
        catch (IdsException ex)
        {
            _logger.LogError(ex, "QuickBooks API error when loading accounts for company {CompanyId}", companyId);
            throw new InvalidOperationException("QuickBooks API returned an error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure when loading QuickBooks accounts for company {CompanyId}", companyId);
            throw new InvalidOperationException("Unable to load QuickBooks chart of accounts.", ex);
        }
    }

    private static QBOAccount MapAccount(Account account)
    {
        var accountType = account.AccountTypeSpecified ? account.AccountType.ToString() : string.Empty;
        var isActive = account.ActiveSpecified ? account.Active : false;
        return new QBOAccount(
            account.Id ?? string.Empty,
            account.Name ?? string.Empty,
            accountType,
            account.AccountSubType,
            isActive);
    }

    private static string GetCacheKey(int companyId) =>
        $"{QuickBooksConstants.CacheConfiguration.ChartOfAccountsCacheKeyPrefix}:{companyId}";
}
