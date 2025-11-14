using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Caskr.Server.Services;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
///     API endpoints that orchestrate the OAuth 2.0 flow between Caskr and QuickBooks Online.
/// </summary>
[Authorize]
[Route("api/accounting/quickbooks")]
public class QuickBooksController(
    IQuickBooksAuthService quickBooksAuthService,
    IUsersService usersService,
    IQuickBooksDataService quickBooksDataService,
    CaskrDbContext dbContext,
    ILogger<QuickBooksController> logger,
    IConfiguration configuration)
    : AuthorizedApiControllerBase
{
    private readonly IQuickBooksAuthService _quickBooksAuthService = quickBooksAuthService;
    private readonly IUsersService _usersService = usersService;
    private readonly IQuickBooksDataService _quickBooksDataService = quickBooksDataService;
    private readonly CaskrDbContext _dbContext = dbContext;
    private readonly ILogger<QuickBooksController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    ///     Initiates the QuickBooks OAuth flow by returning the authorization URL for the specified company.
    /// </summary>
    /// <param name="request">The company identifier payload.</param>
    [HttpPost("connect")]
    [ProducesResponseType(typeof(QuickBooksAuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuickBooksAuthUrlResponse>> Connect([FromBody] QuickBooksCompanyRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new QuickBooksErrorResponse("Request body is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(request.CompanyId);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksAuthUrlResponse>(error);
        }

        try
        {
            var callbackUri = ResolveCallbackUri();
            var authorizationUri = await _quickBooksAuthService.GetAuthorizationUrlAsync(request.CompanyId, callbackUri);
            return Ok(new QuickBooksAuthUrlResponse(authorizationUri.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Unable to build QuickBooks authorization URL for company {CompanyId}", request.CompanyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new QuickBooksErrorResponse("Unable to generate QuickBooks authorization URL."));
        }
    }

    /// <summary>
    ///     Handles the QuickBooks OAuth callback and redirects the user back to the front-end success page.
    /// </summary>
    /// <param name="code">Authorization code returned by QuickBooks.</param>
    /// <param name="realmId">QuickBooks realm/company identifier.</param>
    /// <param name="state">State parameter that contains the Caskr company identifier.</param>
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? realmId, [FromQuery] string? state)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new QuickBooksErrorResponse("QuickBooks callback is missing required parameters."));
        }

        if (!int.TryParse(state, out var companyId))
        {
            return BadRequest(new QuickBooksErrorResponse("Invalid company identifier supplied in the state parameter."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(companyId);
        if (authorizationResult.errorResult is { } error)
        {
            return error;
        }

        try
        {
            await _quickBooksAuthService.HandleCallbackAsync(code, realmId, companyId);
            var redirectUri = BuildSuccessRedirectUrl(companyId, realmId);
            return Redirect(redirectUri);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to process QuickBooks callback for company {CompanyId}", companyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new QuickBooksErrorResponse("QuickBooks authorization failed."));
        }
    }

    /// <summary>
    ///     Revokes QuickBooks access for the supplied company.
    /// </summary>
    /// <param name="request">The company identifier payload.</param>
    [HttpPost("disconnect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Disconnect([FromBody] QuickBooksCompanyRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new QuickBooksErrorResponse("Request body is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(request.CompanyId);
        if (authorizationResult.errorResult is { } error)
        {
            return error;
        }

        try
        {
            await _quickBooksAuthService.RevokeAccessAsync(request.CompanyId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to revoke QuickBooks access for company {CompanyId}", request.CompanyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new QuickBooksErrorResponse("Unable to disconnect QuickBooks."));
        }
    }

    /// <summary>
    ///     Returns the QuickBooks connection status for the supplied company.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    [HttpGet("status")]
    [ProducesResponseType(typeof(QuickBooksStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuickBooksStatusResponse>> GetStatus([FromQuery(Name = "company_id")] int companyId)
    {
        var authorizationResult = await AuthorizeCompanyAsync(companyId);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksStatusResponse>(error);
        }

        try
        {
            var integration = await _dbContext.AccountingIntegrations
                .AsNoTracking()
                .Where(ai => ai.CompanyId == companyId && ai.Provider == AccountingProvider.QuickBooks)
                .OrderByDescending(ai => ai.UpdatedAt)
                .FirstOrDefaultAsync();

            var connected = integration is { IsActive: true };
            DateTime? connectedAt = integration switch
            {
                null => null,
                _ when integration.UpdatedAt != default => integration.UpdatedAt,
                _ => integration.CreatedAt
            };

            return Ok(new QuickBooksStatusResponse(connected, integration?.RealmId, connectedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load QuickBooks status for company {CompanyId}", companyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new QuickBooksErrorResponse("Unable to load QuickBooks status."));
        }
    }

    /// <summary>
    ///     Returns the QuickBooks chart of accounts for the supplied company so the front-end can map Caskr accounts to QuickBooks accounts.
    /// </summary>
    /// <param name="companyId">The identifier of the company whose accounts should be loaded.</param>
    /// <param name="refresh">If true, bypasses the cached chart of accounts and fetches the latest data from QuickBooks.</param>
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(List<QuickBooksAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<QuickBooksAccountResponse>>> GetAccounts(
        [FromQuery(Name = "companyId")] int? companyId,
        [FromQuery(Name = "refresh")] bool refresh = false)
    {
        if (companyId is null || companyId <= 0)
        {
            return BadRequest(new QuickBooksErrorResponse("A valid companyId query parameter is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(companyId.Value);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<List<QuickBooksAccountResponse>>(error);
        }

        var isConnected = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .AnyAsync(ai => ai.CompanyId == companyId.Value && ai.Provider == AccountingProvider.QuickBooks && ai.IsActive);

        if (!isConnected)
        {
            return NotFound(new QuickBooksErrorResponse("QuickBooks not connected for this company"));
        }

        try
        {
            var accounts = await _quickBooksDataService.GetChartOfAccountsAsync(companyId.Value, refresh);
            var response = accounts
                .Select(account => new QuickBooksAccountResponse(account.Id, account.Name, account.AccountType, account.Active))
                .ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load QuickBooks accounts for company {CompanyId}", companyId.Value);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new QuickBooksErrorResponse("Unable to load QuickBooks accounts. Please try again."));
        }
    }

    private async Task<(User? user, IActionResult? errorResult)> AuthorizeCompanyAsync(int companyId)
    {
        if (companyId <= 0)
        {
            return (null, BadRequest(new QuickBooksErrorResponse("A valid company_id is required.")));
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return (null, Unauthorized());
        }

        if (!UserCanAccessCompany(user, companyId))
        {
            return (null, Forbid());
        }

        return (user, null);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _usersService.GetUserByIdAsync(userId);
    }

    private static bool UserCanAccessCompany(User user, int companyId)
    {
        if ((UserType)user.UserTypeId == UserType.SuperAdmin)
        {
            return true;
        }

        return user.CompanyId == companyId;
    }

    private ActionResult<T> ConvertToActionResult<T>(IActionResult error)
    {
        return error switch
        {
            ObjectResult objectResult => StatusCode(objectResult.StatusCode ?? StatusCodes.Status500InternalServerError, objectResult.Value),
            StatusCodeResult statusResult => StatusCode(statusResult.StatusCode),
            ForbidResult => Forbid(),
            ChallengeResult => Challenge(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private string ResolveCallbackUri()
    {
        var scheme = string.IsNullOrWhiteSpace(Request?.Scheme) ? "https" : Request!.Scheme;
        if (Request?.Host.HasValue == true)
        {
            return $"{scheme}://{Request.Host.Value}/api/accounting/quickbooks/callback";
        }

        var configured = _configuration["QuickBooks:RedirectUri"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("QuickBooks redirect URI is not configured.");
        }

        return configured;
    }

    private string BuildSuccessRedirectUrl(int companyId, string realmId)
    {
        var baseUrl = _configuration["QuickBooks:ConnectSuccessRedirectUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "/accounting/quickbooks/success";
        }

        var parameters = new Dictionary<string, string?>
        {
            ["companyId"] = companyId.ToString(CultureInfo.InvariantCulture),
            ["realmId"] = realmId
        };

        return QueryHelpers.AddQueryString(baseUrl, parameters);
    }
}

/// <summary>
///     Payload describing the company that should perform a QuickBooks operation.
/// </summary>
public sealed class QuickBooksCompanyRequest
{
    /// <summary>
    ///     Gets or sets the company identifier.
    /// </summary>
    [JsonPropertyName("company_id")]
    public int CompanyId { get; init; }
}

/// <summary>
///     Response returned when requesting a QuickBooks authorization URL.
/// </summary>
public sealed record QuickBooksAuthUrlResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QuickBooksAuthUrlResponse"/> class.
    /// </summary>
    /// <param name="authUrl">The authorization URL.</param>
    public QuickBooksAuthUrlResponse(string authUrl)
    {
        AuthUrl = authUrl;
    }

    /// <summary>
    ///     Gets the authorization URL for QuickBooks.
    /// </summary>
    [JsonPropertyName("authUrl")]
    public string AuthUrl { get; }
}

/// <summary>
///     Describes the QuickBooks connection status for a company.
/// </summary>
public sealed record QuickBooksStatusResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QuickBooksStatusResponse"/> class.
    /// </summary>
    /// <param name="connected">Whether QuickBooks is connected.</param>
    /// <param name="realmId">The QuickBooks realm identifier.</param>
    /// <param name="connectedAt">When the integration was last connected.</param>
    public QuickBooksStatusResponse(bool connected, string? realmId, DateTime? connectedAt)
    {
        Connected = connected;
        RealmId = realmId;
        ConnectedAt = connectedAt;
    }

    /// <summary>
    ///     Gets a value indicating whether the company is connected to QuickBooks.
    /// </summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; }

    /// <summary>
    ///     Gets the QuickBooks realm identifier, if available.
    /// </summary>
    [JsonPropertyName("realmId")]
    public string? RealmId { get; }

    /// <summary>
    ///     Gets when the company connected to QuickBooks, if available.
    /// </summary>
    [JsonPropertyName("connectedAt")]
    public DateTime? ConnectedAt { get; }
}

/// <summary>
///     Represents an error payload returned by the QuickBooks API endpoints.
/// </summary>
public sealed record QuickBooksErrorResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QuickBooksErrorResponse"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public QuickBooksErrorResponse(string message)
    {
        Message = message;
    }

    /// <summary>
    ///     Gets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; }
}

/// <summary>
///     Represents a simplified QuickBooks account payload exposed to the client.
/// </summary>
public sealed record QuickBooksAccountResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QuickBooksAccountResponse"/> class.
    /// </summary>
    /// <param name="id">The QuickBooks account identifier.</param>
    /// <param name="name">The QuickBooks account name.</param>
    /// <param name="accountType">The QuickBooks account type.</param>
    /// <param name="active">Indicates whether the account is active.</param>
    public QuickBooksAccountResponse(string id, string name, string accountType, bool active)
    {
        Id = id;
        Name = name;
        AccountType = accountType;
        Active = active;
    }

    /// <summary>
    ///     Gets the QuickBooks account identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    ///     Gets the QuickBooks account name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    ///     Gets the QuickBooks account type.
    /// </summary>
    [JsonPropertyName("accountType")]
    public string AccountType { get; }

    /// <summary>
    ///     Gets a value indicating whether the QuickBooks account is active.
    /// </summary>
    [JsonPropertyName("active")]
    public bool Active { get; }
}
