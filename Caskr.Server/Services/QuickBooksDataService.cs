using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.Exception;
using Intuit.Ipp.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Provides data access helpers for QuickBooks Online APIs.
/// </summary>
public class QuickBooksDataService : IQuickBooksDataService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private const string CacheKeyPrefix = "QuickBooksDataService.ChartOfAccounts";

    private readonly IMemoryCache _cache;
    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksAuthService _authService;
    private readonly IQuickBooksAccountQueryClient _accountQueryClient;
    private readonly ILogger<QuickBooksDataService> _logger;

    public QuickBooksDataService(
        IMemoryCache cache,
        CaskrDbContext dbContext,
        IQuickBooksAuthService authService,
        IQuickBooksAccountQueryClient accountQueryClient,
        ILogger<QuickBooksDataService> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _authService = authService;
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

        var integration = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .SingleOrDefaultAsync(ai => ai.CompanyId == companyId && ai.Provider == AccountingProvider.QuickBooks && ai.IsActive);
        if (integration is null)
        {
            throw new InvalidOperationException($"No active QuickBooks integration found for company {companyId}.");
        }

        var tokenResponse = await _authService.RefreshTokenAsync(companyId);
        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("QuickBooks access token is missing.");
        }

        var realmId = !string.IsNullOrWhiteSpace(tokenResponse.RealmId)
            ? tokenResponse.RealmId
            : integration.RealmId;
        if (string.IsNullOrWhiteSpace(realmId))
        {
            throw new InvalidOperationException("QuickBooks realm ID is missing.");
        }

        var serviceContext = CreateServiceContext(realmId, tokenResponse.AccessToken);

        try
        {
            _logger.LogInformation("Fetching QuickBooks chart of accounts for company {CompanyId} (realm {RealmId})", companyId, realmId);
            var qbAccounts = _accountQueryClient.ExecuteActiveAccountQuery(serviceContext);
            var mappedAccounts = qbAccounts.Select(MapAccount).ToList();
            _cache.Set(cacheKey, mappedAccounts, CacheDuration);
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

    private static ServiceContext CreateServiceContext(string realmId, string accessToken)
    {
        var validator = new OAuth2RequestValidator(accessToken);
        return new ServiceContext(realmId, IntuitServicesType.QBO, validator);
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

    private static string GetCacheKey(int companyId) => $"{CacheKeyPrefix}:{companyId}";
}
