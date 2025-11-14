using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Intuit.Ipp.OAuth2PlatformClient;

namespace Caskr.Server.Services;

/// <summary>
///     Factory responsible for constructing QuickBooks OAuth clients backed by the Intuit SDK. The factory keeps all
///     SDK-specific details isolated so higher-level services can focus on orchestration logic and are straightforward to
///     test with mocks.
/// </summary>
public interface IQuickBooksOAuthClientFactory
{
    /// <summary>
    ///     Creates a new OAuth client using the provided credentials.
    /// </summary>
    /// <param name="clientId">QuickBooks application client identifier.</param>
    /// <param name="clientSecret">QuickBooks application client secret.</param>
    /// <param name="redirectUri">Redirect URI registered with Intuit.</param>
    /// <param name="environment">Target QuickBooks environment (sandbox or production).</param>
    /// <returns>A client abstraction that exposes the subset of OAuth operations required by the application.</returns>
    IQuickBooksOAuthClient Create(string clientId, string clientSecret, string redirectUri, string environment);
}

/// <summary>
///     Abstraction over the Intuit SDK's <see cref="OAuth2Client"/> enabling deterministic unit testing.
/// </summary>
public interface IQuickBooksOAuthClient
{
    /// <summary>
    ///     Gets the redirect URI configured for the current OAuth client instance.
    /// </summary>
    string RedirectUri { get; }

    /// <summary>
    ///     Builds the OAuth authorization URL for the supplied scopes and state.
    /// </summary>
    string GetAuthorizationUrl(IEnumerable<string> scopes, string state);

    /// <summary>
    ///     Exchanges an authorization code for OAuth tokens.
    /// </summary>
    Task<TokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes an existing access token using a stored refresh token.
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? realmId, CancellationToken cancellationToken);

    /// <summary>
    ///     Revokes the supplied token, disconnecting the integration.
    /// </summary>
    Task<TokenRevocationResponse> RevokeTokenAsync(string token, CancellationToken cancellationToken);
}

/// <summary>
///     Default implementation that instantiates the Intuit SDK OAuth client.
/// </summary>
public sealed class QuickBooksOAuthClientFactory : IQuickBooksOAuthClientFactory
{
    public IQuickBooksOAuthClient Create(string clientId, string clientSecret, string redirectUri, string environment)
    {
        var sdkClient = new OAuth2Client(clientId, clientSecret, redirectUri, environment);
        return new OAuth2ClientAdapter(sdkClient, redirectUri);
    }

    private sealed class OAuth2ClientAdapter : IQuickBooksOAuthClient
    {
        private readonly OAuth2Client _inner;

        public OAuth2ClientAdapter(OAuth2Client inner, string redirectUri)
        {
            _inner = inner;
            RedirectUri = redirectUri;
        }

        public string RedirectUri { get; }

        public string GetAuthorizationUrl(IEnumerable<string> scopes, string state)
        {
            var scopeList = scopes?.ToList() ?? new List<string>();
            return _inner.GetAuthorizationURL(scopeList, state);
        }

        public Task<TokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
        {
            return _inner.GetBearerTokenAsync(code, RedirectUri, cancellationToken);
        }

        public Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? realmId, CancellationToken cancellationToken)
        {
            return _inner.RefreshTokenAsync(refreshToken, RedirectUri, realmId ?? string.Empty, cancellationToken);
        }

        public Task<TokenRevocationResponse> RevokeTokenAsync(string token, CancellationToken cancellationToken)
        {
            return _inner.RevokeTokenAsync(token, cancellationToken);
        }
    }
}
