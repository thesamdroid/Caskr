using System;
using System.Threading.Tasks;
using Caskr.Server.Models;

namespace Caskr.Server.Services;

/// <summary>
///     Defines the contract for orchestrating the OAuth 2.0 authorization flow with QuickBooks Online.
///     Implementations are responsible for constructing secure authorization URLs, exchanging authorization
///     codes for tokens, refreshing tokens before they expire, and revoking access when an organization
///     disconnects from QuickBooks.
/// </summary>
public interface IQuickBooksAuthService
{
    /// <summary>
    ///     Builds a QuickBooks authorization URL for the provided company so the user can grant access.
    ///     The implementation must embed the supplied redirect URI, a CSRF-resistant state value, and
    ///     any additional scopes that are required for the integration.
    /// </summary>
    /// <param name="companyId">The internal identifier of the company requesting authorization.</param>
    /// <param name="redirectUri">The redirect URI that Intuit should call after the user completes consent.</param>
    /// <returns>
    ///     A <see cref="Uri"/> that the client application can navigate to in order to start the OAuth flow.
    /// </returns>
    Task<Uri> GetAuthorizationUrlAsync(int companyId, string redirectUri);

    /// <summary>
    ///     Handles the callback from QuickBooks after the user approves access by exchanging the authorization
    ///     code for access and refresh tokens. Access tokens expire in approximately one hour, while refresh tokens
    ///     remain valid for up to one hundred days when properly rotated.
    /// </summary>
    /// <param name="code">The authorization code returned by QuickBooks.</param>
    /// <param name="realmId">The QuickBooks company (realm) identifier supplied by the callback.</param>
    /// <param name="companyId">The company that should be linked to the returned tokens.</param>
    /// <returns>
    ///     A populated <see cref="OAuthTokenResponse"/> containing the access token, refresh token, expiration,
    ///     and realm identifier that should be stored securely.
    /// </returns>
    Task<OAuthTokenResponse> HandleCallbackAsync(string code, string realmId, int companyId);

    /// <summary>
    ///     Refreshes the stored access token for the provided company when the current token is expired or near
    ///     expiration. Access tokens last for roughly sixty minutes, so implementations should call this proactively
    ///     and securely persist the new tokens.
    /// </summary>
    /// <param name="companyId">The company whose QuickBooks connection should be refreshed.</param>
    /// <returns>
    ///     A new <see cref="OAuthTokenResponse"/> payload representing the refreshed access and refresh tokens
    ///     returned by QuickBooks.
    /// </returns>
    Task<OAuthTokenResponse> RefreshTokenAsync(int companyId);

    /// <summary>
    ///     Revokes the QuickBooks connection for the given company and deletes any stored tokens so the integration
    ///     can no longer access company data. Implementations should call the Intuit revocation endpoint and ensure
    ///     that secrets are securely removed from storage.
    /// </summary>
    /// <param name="companyId">The company whose QuickBooks access should be revoked.</param>
    Task RevokeAccessAsync(int companyId);
}
