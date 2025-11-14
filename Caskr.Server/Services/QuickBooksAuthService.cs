using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Caskr.Server.Models;
using Caskr.server.Models;
using Intuit.Ipp.OAuth2PlatformClient;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Handles the OAuth 2.0 flow with QuickBooks Online, including authorization URL generation, token exchange,
///     token refresh, and revocation. All sensitive tokens are protected using ASP.NET Core Data Protection before
///     being persisted to the database.
/// </summary>
public class QuickBooksAuthService : IQuickBooksAuthService
{
    private const string AccountingScope = "com.intuit.quickbooks.accounting";
    private const string ProtectorPurpose = "Caskr.Server.Services.QuickBooksAuthService.Tokens";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan TokenExpirySkew = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly CaskrDbContext _dbContext;
    private readonly IDataProtector _tokenProtector;
    private readonly ILogger<QuickBooksAuthService> _logger;
    private readonly IQuickBooksOAuthClientFactory _clientFactory;

    public QuickBooksAuthService(
        IConfiguration configuration,
        CaskrDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<QuickBooksAuthService> logger,
        IQuickBooksOAuthClientFactory clientFactory)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _tokenProtector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _logger = logger;
        _clientFactory = clientFactory;
    }

    public Task<Uri> GetAuthorizationUrlAsync(int companyId, string redirectUri)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId));
        }

        var resolvedRedirect = ResolveRedirectUri(redirectUri);
        var client = CreateOAuthClient(resolvedRedirect);

        try
        {
            _logger.LogInformation("Generating QuickBooks authorization URL for company {CompanyId}", companyId);
            var url = client.GetAuthorizationUrl(new[] { AccountingScope }, companyId.ToString(CultureInfo.InvariantCulture));
            if (!Uri.TryCreate(url, UriKind.Absolute, out var authorizationUri))
            {
                throw new InvalidOperationException("QuickBooks returned an invalid authorization URL.");
            }

            return Task.FromResult(authorizationUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build QuickBooks authorization URL for company {CompanyId}", companyId);
            throw new InvalidOperationException("Unable to generate QuickBooks authorization URL.", ex);
        }
    }

    public async Task<OAuthTokenResponse> HandleCallbackAsync(string code, string realmId, int companyId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Authorization code is required.", nameof(code));
        }

        var resolvedRedirect = ResolveRedirectUri(null);
        var client = CreateOAuthClient(resolvedRedirect);

        try
        {
            _logger.LogInformation("Exchanging QuickBooks authorization code for company {CompanyId}", companyId);
            var tokenResponse = await client.ExchangeCodeForTokenAsync(code, CancellationToken.None);

            var integration = await GetOrCreateIntegrationAsync(companyId);
            PersistTokens(integration, tokenResponse, realmId);
            await _dbContext.SaveChangesAsync();

            return MapToDto(tokenResponse, realmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle QuickBooks OAuth callback for company {CompanyId}", companyId);
            throw new InvalidOperationException("QuickBooks authorization failed.", ex);
        }
    }

    public async Task<OAuthTokenResponse> RefreshTokenAsync(int companyId)
    {
        var integration = await GetIntegrationAsync(companyId);
        if (integration is null)
        {
            throw new InvalidOperationException($"No QuickBooks integration found for company {companyId}.");
        }

        var accessTokenPayload = ReadTokenPayload(integration.AccessTokenEncrypted, "access token");
        var refreshTokenPayload = ReadTokenPayload(integration.RefreshTokenEncrypted, "refresh token");

        if (!IsTokenExpired(integration, accessTokenPayload))
        {
            _logger.LogInformation("QuickBooks access token still valid for company {CompanyId}", companyId);
            return new OAuthTokenResponse
            {
                AccessToken = accessTokenPayload.Token,
                RefreshToken = refreshTokenPayload.Token,
                ExpiresIn = Convert.ToInt32(Math.Min(accessTokenPayload.ExpiresInSeconds, int.MaxValue)),
                RealmId = integration.RealmId ?? string.Empty
            };
        }

        try
        {
            _logger.LogInformation("Refreshing QuickBooks token for company {CompanyId}", companyId);
            var resolvedRedirect = ResolveRedirectUri(null);
            var client = CreateOAuthClient(resolvedRedirect);
            var response = await client.RefreshTokenAsync(refreshTokenPayload.Token, integration.RealmId, CancellationToken.None);

            PersistTokens(integration, response, integration.RealmId ?? string.Empty);
            await _dbContext.SaveChangesAsync();

            return MapToDto(response, integration.RealmId ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh QuickBooks token for company {CompanyId}", companyId);
            throw new InvalidOperationException("Unable to refresh QuickBooks tokens.", ex);
        }
    }

    public async Task RevokeAccessAsync(int companyId)
    {
        var integration = await GetIntegrationAsync(companyId);
        if (integration is null)
        {
            _logger.LogInformation("No QuickBooks integration to revoke for company {CompanyId}", companyId);
            return;
        }

        try
        {
            var refreshPayload = string.IsNullOrWhiteSpace(integration.RefreshTokenEncrypted)
                ? null
                : ReadTokenPayload(integration.RefreshTokenEncrypted, "refresh token");
            if (refreshPayload != null)
            {
                var resolvedRedirect = ResolveRedirectUri(null);
                var client = CreateOAuthClient(resolvedRedirect);
                await client.RevokeTokenAsync(refreshPayload.Token, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke QuickBooks tokens for company {CompanyId}", companyId);
        }

        integration.IsActive = false;
        integration.AccessTokenEncrypted = null;
        integration.RefreshTokenEncrypted = null;
        integration.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private IQuickBooksOAuthClient CreateOAuthClient(string redirectUri)
    {
        var (clientId, clientSecret, environment) = GetClientConfiguration();
        return _clientFactory.Create(clientId, clientSecret, redirectUri, environment);
    }

    private (string clientId, string clientSecret, string environment) GetClientConfiguration()
    {
        var clientId = _configuration["QuickBooks:ClientId"];
        var clientSecret = _configuration["QuickBooks:ClientSecret"];
        var environment = _configuration["QuickBooks:Environment"] ?? "sandbox";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("QuickBooks client credentials are not configured.");
        }

        return (clientId, clientSecret, environment);
    }

    private async Task<AccountingIntegration> GetOrCreateIntegrationAsync(int companyId)
    {
        var integration = await _dbContext.AccountingIntegrations
            .SingleOrDefaultAsync(ai => ai.CompanyId == companyId && ai.Provider == AccountingProvider.QuickBooks);

        if (integration != null)
        {
            return integration;
        }

        integration = new AccountingIntegration
        {
            CompanyId = companyId,
            Provider = AccountingProvider.QuickBooks,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AccountingIntegrations.Add(integration);
        return integration;
    }

    private Task<AccountingIntegration?> GetIntegrationAsync(int companyId)
    {
        return _dbContext.AccountingIntegrations
            .SingleOrDefaultAsync(ai => ai.CompanyId == companyId && ai.Provider == AccountingProvider.QuickBooks);
    }

    private void PersistTokens(AccountingIntegration integration, TokenResponse response, string realmId)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken) || string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new InvalidOperationException("QuickBooks did not return both access and refresh tokens.");
        }

        integration.AccessTokenEncrypted = ProtectToken(response.AccessToken, response.AccessTokenExpiresIn);
        integration.RefreshTokenEncrypted = ProtectToken(response.RefreshToken, response.RefreshTokenExpiresIn);
        integration.RealmId = realmId;
        integration.IsActive = true;
        integration.UpdatedAt = DateTime.UtcNow;
        if (integration.CreatedAt == default)
        {
            integration.CreatedAt = integration.UpdatedAt;
        }
    }

    private OAuthTokenResponse MapToDto(TokenResponse response, string realmId)
    {
        return new OAuthTokenResponse
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresIn = Convert.ToInt32(Math.Min(response.AccessTokenExpiresIn, int.MaxValue)),
            RealmId = realmId
        };
    }

    private string ProtectToken(string token, long expiresInSeconds)
    {
        var payload = new TokenPayload(token, expiresInSeconds);
        var serialized = JsonSerializer.Serialize(payload, SerializerOptions);
        return _tokenProtector.Protect(serialized);
    }

    private TokenPayload ReadTokenPayload(string? encryptedValue, string tokenDescription)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            throw new InvalidOperationException($"QuickBooks {tokenDescription} is missing.");
        }

        try
        {
            var json = _tokenProtector.Unprotect(encryptedValue);
            var payload = JsonSerializer.Deserialize<TokenPayload>(json, SerializerOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
            {
                throw new InvalidOperationException("Token payload missing required values.");
            }

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to decrypt QuickBooks {TokenDescription} payload", tokenDescription);
            throw new InvalidOperationException("Unable to read stored QuickBooks tokens.", ex);
        }
    }

    private bool IsTokenExpired(AccountingIntegration integration, TokenPayload payload)
    {
        var issuedAt = integration.UpdatedAt != default ? integration.UpdatedAt : integration.CreatedAt;
        if (issuedAt == default)
        {
            return true;
        }

        var expiry = issuedAt.AddSeconds(payload.ExpiresInSeconds);
        var safeExpiry = expiry - TokenExpirySkew;
        if (safeExpiry <= issuedAt)
        {
            safeExpiry = issuedAt;
        }

        return DateTime.UtcNow >= safeExpiry;
    }

    private string ResolveRedirectUri(string? overrideValue)
    {
        var redirectUri = !string.IsNullOrWhiteSpace(overrideValue)
            ? overrideValue
            : _configuration["QuickBooks:RedirectUri"];

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("QuickBooks redirect URI is not configured.");
        }

        return redirectUri;
    }

    private sealed record TokenPayload(string Token, long ExpiresInSeconds);
}
