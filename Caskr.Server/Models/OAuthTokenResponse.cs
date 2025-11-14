namespace Caskr.Server.Models;

/// <summary>
///     Represents the OAuth tokens returned by QuickBooks. Access tokens typically expire in one hour and should be
///     refreshed well before their expiration time, while refresh tokens can be rotated for up to one hundred days and
///     must be stored with the same level of protection as any other credential material.
/// </summary>
public class OAuthTokenResponse
{
    /// <summary>
    ///     Gets or sets the short-lived access token used to call QuickBooks APIs. Expires after roughly sixty minutes.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the refresh token that can be exchanged for new access tokens for up to one hundred days when
    ///     rotated regularly.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the number of seconds before the access token expires. Use this to schedule proactive refreshes.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    ///     Gets or sets the QuickBooks realm (company) identifier associated with the tokens.
    /// </summary>
    public string RealmId { get; set; } = string.Empty;
}
