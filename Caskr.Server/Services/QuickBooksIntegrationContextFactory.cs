using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Caskr.server.Models;
using Caskr.Server.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Builds fully-initialized <see cref="ServiceContext" /> instances for QuickBooks API calls.
///     The factory centralizes token refresh, realm validation, and guards to ensure only
///     active integrations can be used by downstream services.
/// </summary>
public interface IQuickBooksIntegrationContextFactory
{
    /// <summary>
    ///     Creates an authenticated QuickBooks <see cref="ServiceContext" /> for the supplied company.
    /// </summary>
    /// <param name="companyId">Company whose QuickBooks connection should be used.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task<QuickBooksIntegrationContext> CreateAsync(int companyId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Represents the hydrated context required to execute QuickBooks SDK requests.
/// </summary>
/// <param name="CompanyId">The owning company identifier.</param>
/// <param name="RealmId">QuickBooks realm identifier.</param>
/// <param name="ServiceContext">Authenticated service context.</param>
public sealed record QuickBooksIntegrationContext(int CompanyId, string RealmId, ServiceContext ServiceContext);

/// <summary>
///     Default implementation of <see cref="IQuickBooksIntegrationContextFactory" />.
/// </summary>
[AutoBind]
public sealed class QuickBooksIntegrationContextFactory : IQuickBooksIntegrationContextFactory
{
    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksAuthService _authService;
    private readonly ILogger<QuickBooksIntegrationContextFactory> _logger;

    public QuickBooksIntegrationContextFactory(
        CaskrDbContext dbContext,
        IQuickBooksAuthService authService,
        ILogger<QuickBooksIntegrationContextFactory> logger)
    {
        _dbContext = dbContext;
        _authService = authService;
        _logger = logger;
    }

    public async Task<QuickBooksIntegrationContext> CreateAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId));
        }

        var integration = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                ai => ai.CompanyId == companyId
                      && ai.Provider == AccountingProvider.QuickBooks
                      && ai.IsActive,
                cancellationToken);

        if (integration is null)
        {
            throw new InvalidOperationException($"Company {companyId} does not have an active QuickBooks integration.");
        }

        OAuthTokenResponse tokenResponse;
        try
        {
            tokenResponse = await _authService.RefreshTokenAsync(companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh QuickBooks tokens for company {CompanyId}", companyId);
            throw;
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("QuickBooks access token is missing.");
        }

        var realmId = !string.IsNullOrWhiteSpace(tokenResponse.RealmId)
            ? tokenResponse.RealmId!
            : integration.RealmId;

        if (string.IsNullOrWhiteSpace(realmId))
        {
            throw new InvalidOperationException("QuickBooks realm ID is missing.");
        }

        var validator = new OAuth2RequestValidator(tokenResponse.AccessToken);
        var serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, validator);
        return new QuickBooksIntegrationContext(companyId, realmId, serviceContext);
    }
}
