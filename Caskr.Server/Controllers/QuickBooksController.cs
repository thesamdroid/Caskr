using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Caskr.Server.Models;
using Caskr.Server.Services;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    IConfiguration configuration,
    IMemoryCache memoryCache,
    IQuickBooksInvoiceSyncService quickBooksInvoiceSyncService)
    : AuthorizedApiControllerBase
{

    private readonly IQuickBooksAuthService _quickBooksAuthService = quickBooksAuthService;
    private readonly IUsersService _usersService = usersService;
    private readonly IQuickBooksDataService _quickBooksDataService = quickBooksDataService;
    private readonly CaskrDbContext _dbContext = dbContext;
    private readonly ILogger<QuickBooksController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IQuickBooksInvoiceSyncService _invoiceSyncService = quickBooksInvoiceSyncService;

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
    ///     Returns the most recent sync status for an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    [HttpGet("invoice-status")]
    [ProducesResponseType(typeof(QuickBooksInvoiceSyncStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuickBooksInvoiceSyncStatusResponse>> GetInvoiceSyncStatus([FromQuery(Name = "invoiceId")] int? invoiceId)
    {
        if (invoiceId is null || invoiceId <= 0)
        {
            return BadRequest(new QuickBooksErrorResponse("A valid invoiceId query parameter is required."));
        }

        var authorizationResult = await AuthorizeInvoiceAsync(invoiceId.Value);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksInvoiceSyncStatusResponse>(error);
        }

        try
        {
            var latestLog = await _dbContext.AccountingSyncLogs
                .AsNoTracking()
                .Where(log => log.CompanyId == authorizationResult.invoice!.CompanyId
                              && log.EntityType == QuickBooksConstants.EntityTypes.Invoice
                              && log.EntityId == invoiceId.Value.ToString(CultureInfo.InvariantCulture))
                .OrderByDescending(log => log.UpdatedAt)
                .FirstOrDefaultAsync();

            var response = latestLog is null
                ? new QuickBooksInvoiceSyncStatusResponse(invoiceId.Value, null, null, null, null)
                : new QuickBooksInvoiceSyncStatusResponse(
                    invoiceId.Value,
                    latestLog.SyncStatus,
                    latestLog.ExternalEntityId,
                    latestLog.ErrorMessage,
                    latestLog.SyncedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load QuickBooks sync status for invoice {InvoiceId}", invoiceId.Value);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new QuickBooksErrorResponse("Unable to load QuickBooks sync status."));
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

    /// <summary>
    ///     Saves the QuickBooks chart of accounts mappings for a company.
    /// </summary>
    /// <param name="request">The mapping payload.</param>
    [HttpPost("mappings")]
    [ProducesResponseType(typeof(List<QuickBooksAccountMappingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<QuickBooksAccountMappingResponse>>> SaveMappings(
        [FromBody] QuickBooksMappingRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new QuickBooksErrorResponse("Request body is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(request.CompanyId);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<List<QuickBooksAccountMappingResponse>>(error);
        }

        if (request.Mappings is null || request.Mappings.Count == 0)
        {
            return BadRequest(new QuickBooksErrorResponse("At least one mapping is required."));
        }

        var parsedMappings = new List<(CaskrAccountType Type, string QboAccountId, string? QboAccountName)>();
        var mappedTypes = new HashSet<CaskrAccountType>();

        foreach (var mapping in request.Mappings)
        {
            if (mapping is null)
            {
                return BadRequest(new QuickBooksErrorResponse("Each mapping entry is required."));
            }

            if (string.IsNullOrWhiteSpace(mapping.CaskrAccountType))
            {
                return BadRequest(new QuickBooksErrorResponse("Caskr account type is required for each mapping."));
            }

            if (!Enum.TryParse(mapping.CaskrAccountType, true, out CaskrAccountType caskrAccountType))
            {
                return BadRequest(new QuickBooksErrorResponse($"Invalid Caskr account type '{mapping.CaskrAccountType}'."));
            }

            if (!mappedTypes.Add(caskrAccountType))
            {
                return BadRequest(new QuickBooksErrorResponse($"{FormatAccountType(caskrAccountType)} account may only be mapped once."));
            }

            if (string.IsNullOrWhiteSpace(mapping.QboAccountId))
            {
                return BadRequest(new QuickBooksErrorResponse($"{FormatAccountType(caskrAccountType)} account must include a QuickBooks account ID."));
            }

            parsedMappings.Add((caskrAccountType, mapping.QboAccountId, mapping.QboAccountName));
        }

        var requiredTypes = Enum.GetValues<CaskrAccountType>();
        var missingType = requiredTypes.FirstOrDefault(type => !mappedTypes.Contains(type));
        if (!mappedTypes.SetEquals(requiredTypes))
        {
            return BadRequest(new QuickBooksErrorResponse($"{FormatAccountType(missingType)} account must be mapped."));
        }

        var isConnected = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .AnyAsync(ai => ai.CompanyId == request.CompanyId && ai.Provider == AccountingProvider.QuickBooks && ai.IsActive);

        if (!isConnected)
        {
            return NotFound(new QuickBooksErrorResponse("QuickBooks not connected for this company"));
        }

        List<QBOAccount> accounts;
        try
        {
            accounts = await _quickBooksDataService.GetChartOfAccountsAsync(request.CompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load QuickBooks accounts for company {CompanyId}", request.CompanyId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new QuickBooksErrorResponse("Unable to load QuickBooks accounts. Please try again."));
        }

        var availableAccounts = accounts.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var mapping in parsedMappings)
        {
            if (!availableAccounts.Contains(mapping.QboAccountId))
            {
                return BadRequest(new QuickBooksErrorResponse($"QuickBooks account '{mapping.QboAccountId}' was not found."));
            }
        }

        var existingMappings = await _dbContext.ChartOfAccountsMappings
            .Where(m => m.CompanyId == request.CompanyId)
            .ToListAsync();

        if (existingMappings.Count > 0)
        {
            _dbContext.ChartOfAccountsMappings.RemoveRange(existingMappings);
        }

        var now = DateTime.UtcNow;
        var entities = parsedMappings
            .Select(mapping => new ChartOfAccountsMapping
            {
                CompanyId = request.CompanyId,
                CaskrAccountType = mapping.Type,
                QboAccountId = mapping.QboAccountId,
                QboAccountName = mapping.QboAccountName,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        _dbContext.ChartOfAccountsMappings.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        var response = entities
            .Select(e => new QuickBooksAccountMappingResponse(e.CaskrAccountType.ToString(), e.QboAccountId, e.QboAccountName))
            .ToList();

        if (authorizationResult.user is { } user)
        {
            _logger.LogInformation(
                "User {UserId} updated QuickBooks mappings for company {CompanyId}: {@Mappings}",
                user.Id,
                request.CompanyId,
                response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Manually synchronizes a specific invoice with QuickBooks Online.
    /// </summary>
    /// <param name="request">The invoice sync payload.</param>
    [HttpPost("sync-invoice")]
    [ProducesResponseType(typeof(QuickBooksInvoiceSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(QuickBooksInvoiceSyncResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuickBooksInvoiceSyncResponse>> SyncInvoice([FromBody] QuickBooksInvoiceSyncRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new QuickBooksErrorResponse("Request body is required."));
        }

        var authorizationResult = await AuthorizeInvoiceAsync(request.InvoiceId);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksInvoiceSyncResponse>(error);
        }

        var invoice = authorizationResult.invoice!;
        var hasIntegration = await CompanyHasQuickBooksIntegrationAsync(invoice.CompanyId);
        if (!hasIntegration)
        {
            return BadRequest(new QuickBooksErrorResponse("QuickBooks is not connected for this company."));
        }

        if (IsRateLimited(invoice.CompanyId))
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new QuickBooksErrorResponse("Too many QuickBooks sync requests. Please wait a moment and try again."));
        }

        var now = DateTime.UtcNow;
        var inProgressCutoff = now - QuickBooksConstants.SyncConfiguration.InProgressWindow;
        var recentInProgress = await _dbContext.AccountingSyncLogs
            .AsNoTracking()
            .Where(log => log.CompanyId == invoice.CompanyId
                          && log.EntityType == QuickBooksConstants.EntityTypes.Invoice
                          && log.EntityId == invoice.Id.ToString(CultureInfo.InvariantCulture)
                          && log.SyncStatus == SyncStatus.InProgress
                          && log.SyncedAt >= inProgressCutoff)
            .FirstOrDefaultAsync();

        if (recentInProgress is not null)
        {
            return Conflict(new QuickBooksErrorResponse("Sync already in progress"));
        }

        try
        {
            var result = await _invoiceSyncService.SyncInvoiceToQBOAsync(invoice.Id);
            if (result.Success)
            {
                var successResponse = new QuickBooksInvoiceSyncResponse(
                    invoice.Id,
                    true,
                    result.QboInvoiceId,
                    null,
                    SyncStatus.Success,
                    DateTime.UtcNow);
                return Ok(successResponse);
            }

            var failureResponse = new QuickBooksInvoiceSyncResponse(
                invoice.Id,
                false,
                result.QboInvoiceId,
                result.ErrorMessage ?? "QuickBooks sync failed.",
                SyncStatus.Failed,
                DateTime.UtcNow);
            return StatusCode(StatusCodes.Status500InternalServerError, failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks invoice sync failed for invoice {InvoiceId}", invoice.Id);
            var errorResponse = new QuickBooksInvoiceSyncResponse(
                invoice.Id,
                false,
                null,
                "QuickBooks sync failed.",
                SyncStatus.Failed,
                DateTime.UtcNow);
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    ///     Retrieves the saved QuickBooks sync preferences for a company.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(QuickBooksSyncPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuickBooksSyncPreferencesResponse>> GetSyncPreferences([FromQuery(Name = "companyId")] int? companyId)
    {
        if (companyId is null || companyId <= 0)
        {
            return BadRequest(new QuickBooksErrorResponse("A valid companyId query parameter is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(companyId.Value);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksSyncPreferencesResponse>(error);
        }

        var hasIntegration = await CompanyHasQuickBooksIntegrationAsync(companyId.Value);
        if (!hasIntegration)
        {
            return NotFound(new QuickBooksErrorResponse("QuickBooks not connected for this company."));
        }

        var preference = await _dbContext.AccountingSyncPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.CompanyId == companyId.Value && p.Provider == AccountingProvider.QuickBooks);

        if (preference is null)
        {
            return NotFound(new QuickBooksErrorResponse("QuickBooks sync preferences not configured."));
        }

        return Ok(MapPreferences(preference));
    }

    /// <summary>
    ///     Creates or updates the QuickBooks sync preferences for a company.
    /// </summary>
    /// <param name="request">The sync preference payload.</param>
    [HttpPost("preferences")]
    [ProducesResponseType(typeof(QuickBooksSyncPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuickBooksSyncPreferencesResponse>> SaveSyncPreferences([FromBody] QuickBooksSyncPreferencesRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new QuickBooksErrorResponse("Request body is required."));
        }

        if (request.CompanyId <= 0)
        {
            return BadRequest(new QuickBooksErrorResponse("A valid companyId is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.SyncFrequency) && !IsValidSyncFrequency(request.SyncFrequency))
        {
            var allowed = string.Join(", ", QuickBooksConstants.SyncFrequencies.AllAllowed);
            return BadRequest(new QuickBooksErrorResponse($"Sync frequency must be one of: {allowed}."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(request.CompanyId);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksSyncPreferencesResponse>(error);
        }

        var hasIntegration = await CompanyHasQuickBooksIntegrationAsync(request.CompanyId);
        if (!hasIntegration)
        {
            return BadRequest(new QuickBooksErrorResponse("QuickBooks is not connected for this company."));
        }

        var normalizedFrequency = NormalizeSyncFrequency(request.SyncFrequency);
        var preference = await _dbContext.AccountingSyncPreferences
            .SingleOrDefaultAsync(p => p.CompanyId == request.CompanyId && p.Provider == AccountingProvider.QuickBooks);

        if (preference is null)
        {
            preference = new AccountingSyncPreference
            {
                CompanyId = request.CompanyId,
                Provider = AccountingProvider.QuickBooks,
                AutoSyncInvoices = request.AutoSyncInvoices,
                AutoSyncCogs = request.AutoSyncCogs,
                SyncFrequency = normalizedFrequency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.AccountingSyncPreferences.Add(preference);
        }
        else
        {
            preference.AutoSyncInvoices = request.AutoSyncInvoices;
            preference.AutoSyncCogs = request.AutoSyncCogs;
            preference.SyncFrequency = normalizedFrequency;
            preference.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(MapPreferences(preference));
    }

    /// <summary>
    ///     Performs a lightweight QuickBooks connectivity verification for the specified company.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    [HttpGet("test")]
    [ProducesResponseType(typeof(QuickBooksConnectionTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuickBooksErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuickBooksConnectionTestResponse>> TestQuickBooksConnection([FromQuery(Name = "companyId")] int? companyId)
    {
        if (companyId is null || companyId <= 0)
        {
            return BadRequest(new QuickBooksErrorResponse("A valid companyId query parameter is required."));
        }

        var authorizationResult = await AuthorizeCompanyAsync(companyId.Value);
        if (authorizationResult.errorResult is { } error)
        {
            return ConvertToActionResult<QuickBooksConnectionTestResponse>(error);
        }

        var hasIntegration = await CompanyHasQuickBooksIntegrationAsync(companyId.Value);
        if (!hasIntegration)
        {
            return Ok(new QuickBooksConnectionTestResponse(false, "QuickBooks is not connected for this company."));
        }

        try
        {
            await _quickBooksDataService.GetChartOfAccountsAsync(companyId.Value, true);
            return Ok(new QuickBooksConnectionTestResponse(true, "QuickBooks connection verified."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QuickBooks connection test failed for company {CompanyId}", companyId.Value);
            return Ok(new QuickBooksConnectionTestResponse(false, "Unable to verify QuickBooks connection."));
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

        var configured = _configuration[QuickBooksConstants.ConfigurationKeys.RedirectUri];
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("QuickBooks redirect URI is not configured.");
        }

        return configured;
    }

    private string BuildSuccessRedirectUrl(int companyId, string realmId)
    {
        var baseUrl = _configuration[QuickBooksConstants.ConfigurationKeys.ConnectSuccessRedirectUrl];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = QuickBooksConstants.ConfigurationKeys.DefaultConnectSuccessRedirectUrl;
        }

        var parameters = new Dictionary<string, string?>
        {
            ["companyId"] = companyId.ToString(CultureInfo.InvariantCulture),
            ["realmId"] = realmId
        };

        return QueryHelpers.AddQueryString(baseUrl, parameters);
    }

    private static string FormatAccountType(CaskrAccountType accountType)
    {
        return accountType switch
        {
            CaskrAccountType.Cogs => "COGS",
            CaskrAccountType.WorkInProgress => "Work in Progress",
            CaskrAccountType.FinishedGoods => "Finished Goods",
            CaskrAccountType.RawMaterials => "Raw Materials",
            CaskrAccountType.Barrels => "Barrels",
            CaskrAccountType.Ingredients => "Ingredients",
            CaskrAccountType.Labor => "Labor",
            CaskrAccountType.Overhead => "Overhead",
            _ => accountType.ToString()
        };
    }

    private async Task<(Invoice? invoice, IActionResult? errorResult)> AuthorizeInvoiceAsync(int invoiceId)
    {
        if (invoiceId <= 0)
        {
            return (null, BadRequest(new QuickBooksErrorResponse("A valid invoiceId is required.")));
        }

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice is null)
        {
            return (null, NotFound(new QuickBooksErrorResponse($"Invoice {invoiceId} was not found.")));
        }

        var authorizationResult = await AuthorizeCompanyAsync(invoice.CompanyId);
        if (authorizationResult.errorResult is { } error)
        {
            return (null, error);
        }

        return (invoice, null);
    }

    private async Task<bool> CompanyHasQuickBooksIntegrationAsync(int companyId)
    {
        return await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .AnyAsync(ai => ai.CompanyId == companyId && ai.Provider == AccountingProvider.QuickBooks && ai.IsActive);
    }

    private static bool IsValidSyncFrequency(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return false;
        }

        return QuickBooksConstants.SyncFrequencies.AllAllowed.Any(value =>
            string.Equals(value, frequency, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSyncFrequency(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return QuickBooksConstants.SyncFrequencies.Default;
        }

        return QuickBooksConstants.SyncFrequencies.AllAllowed.First(value =>
            string.Equals(value, frequency, StringComparison.OrdinalIgnoreCase));
    }

    private static QuickBooksSyncPreferencesResponse MapPreferences(AccountingSyncPreference preference)
    {
        return new QuickBooksSyncPreferencesResponse(
            preference.CompanyId,
            preference.AutoSyncInvoices,
            preference.AutoSyncCogs,
            string.IsNullOrWhiteSpace(preference.SyncFrequency)
                ? QuickBooksConstants.SyncFrequencies.Default
                : preference.SyncFrequency);
    }

    private bool IsRateLimited(int companyId)
    {
        var cacheKey = $"{QuickBooksConstants.RateLimiting.CacheKeyPrefix}{companyId}";
        var now = DateTime.UtcNow;
        var windowStart = now - QuickBooksConstants.RateLimiting.Window;

        var timestamps = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = QuickBooksConstants.RateLimiting.Window;
            return new List<DateTime>();
        })!;

        timestamps.RemoveAll(timestamp => timestamp < windowStart);
        if (timestamps.Count >= QuickBooksConstants.RateLimiting.MaxRequestsPerWindow)
        {
            return true;
        }

        timestamps.Add(now);
        _memoryCache.Set(cacheKey, timestamps, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = QuickBooksConstants.RateLimiting.Window
        });

        return false;
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

/// <summary>
///     Request payload for saving QuickBooks account mappings.
/// </summary>
public sealed class QuickBooksMappingRequest
{
    /// <summary>
    ///     Gets or sets the company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; }

    /// <summary>
    ///     Gets or sets the mappings payload.
    /// </summary>
    [JsonPropertyName("mappings")]
    public List<QuickBooksAccountMappingDto> Mappings { get; set; } = new();
}

/// <summary>
///     Represents a mapping between a Caskr account and a QuickBooks account.
/// </summary>
public sealed class QuickBooksAccountMappingDto
{
    /// <summary>
    ///     Gets or sets the Caskr account type.
    /// </summary>
    [JsonPropertyName("caskrAccountType")]
    public string? CaskrAccountType { get; set; }

    /// <summary>
    ///     Gets or sets the QuickBooks account identifier.
    /// </summary>
    [JsonPropertyName("qboAccountId")]
    public string? QboAccountId { get; set; }

    /// <summary>
    ///     Gets or sets the QuickBooks account name.
    /// </summary>
    [JsonPropertyName("qboAccountName")]
    public string? QboAccountName { get; set; }
}

/// <summary>
///     Response payload returned after saving QuickBooks account mappings.
/// </summary>
public sealed record QuickBooksAccountMappingResponse(
    [property: JsonPropertyName("caskrAccountType")] string CaskrAccountType,
    [property: JsonPropertyName("qboAccountId")] string QboAccountId,
    [property: JsonPropertyName("qboAccountName")] string? QboAccountName);

/// <summary>
///     Request payload describing QuickBooks sync preferences.
/// </summary>
public sealed class QuickBooksSyncPreferencesRequest
{
    /// <summary>
    ///     Gets or sets the company identifier.
    /// </summary>
    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; }

    /// <summary>
    ///     Gets or sets whether invoices should automatically sync.
    /// </summary>
    [JsonPropertyName("autoSyncInvoices")]
    public bool AutoSyncInvoices { get; set; }

    /// <summary>
    ///     Gets or sets whether COGS should automatically sync.
    /// </summary>
    [JsonPropertyName("autoSyncCogs")]
    public bool AutoSyncCogs { get; set; }

    /// <summary>
    ///     Gets or sets the preferred sync frequency.
    /// </summary>
    [JsonPropertyName("syncFrequency")]
    public string? SyncFrequency { get; set; }
}

/// <summary>
///     Response payload describing the saved QuickBooks sync preferences.
/// </summary>
public sealed record QuickBooksSyncPreferencesResponse(
    [property: JsonPropertyName("companyId")] int CompanyId,
    [property: JsonPropertyName("autoSyncInvoices")] bool AutoSyncInvoices,
    [property: JsonPropertyName("autoSyncCogs")] bool AutoSyncCogs,
    [property: JsonPropertyName("syncFrequency")] string SyncFrequency);

/// <summary>
///     Response payload returned after testing the QuickBooks connection.
/// </summary>
public sealed record QuickBooksConnectionTestResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);

/// <summary>
///     Represents the QuickBooks sync status for a specific invoice.
/// </summary>
public sealed record QuickBooksInvoiceSyncStatusResponse(
    [property: JsonPropertyName("invoiceId")] int InvoiceId,
    [property: JsonPropertyName("status")] SyncStatus? Status,
    [property: JsonPropertyName("qboInvoiceId")] string? QboInvoiceId,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("lastSyncedAt")] DateTime? LastSyncedAt);

/// <summary>
///     Request payload describing which invoice should be synchronized with QuickBooks.
/// </summary>
public sealed class QuickBooksInvoiceSyncRequest
{
    /// <summary>
    ///     Gets or sets the invoice identifier to sync.
    /// </summary>
    [JsonPropertyName("invoiceId")]
    public int InvoiceId { get; set; }
}

/// <summary>
///     Response payload returned after attempting to sync an invoice with QuickBooks.
/// </summary>
public sealed record QuickBooksInvoiceSyncResponse(
    [property: JsonPropertyName("invoiceId")] int InvoiceId,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("qboInvoiceId")] string? QboInvoiceId,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("status")] SyncStatus? Status,
    [property: JsonPropertyName("lastSyncedAt")] DateTime? LastSyncedAt);
