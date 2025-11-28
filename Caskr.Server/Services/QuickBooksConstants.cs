using System;

namespace Caskr.Server.Services;

/// <summary>
///     Centralized constants and configuration values for the QuickBooks integration.
///     This ensures consistency across all QuickBooks services and makes maintenance easier.
/// </summary>
public static class QuickBooksConstants
{
    /// <summary>
    ///     Entity type identifiers used in accounting_sync_logs table.
    /// </summary>
    public static class EntityTypes
    {
        public const string Invoice = "Invoice";
        public const string Batch = "Batch";
        public const string Payment = "Payment";
    }

    /// <summary>
    ///     Retry configuration for transient failures.
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        ///     Maximum number of retry attempts for transient failures.
        /// </summary>
        public const int MaxRetryCount = 3;

        /// <summary>
        ///     Initial delay before the first retry attempt.
        /// </summary>
        public static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        ///     Multiplier for exponential backoff between retries.
        /// </summary>
        public const double BackoffMultiplier = 2.0;
    }

    /// <summary>
    ///     Cache configuration for QuickBooks data.
    /// </summary>
    public static class CacheConfiguration
    {
        /// <summary>
        ///     Duration to cache the chart of accounts.
        /// </summary>
        public static readonly TimeSpan ChartOfAccountsCacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        ///     Cache key prefix for chart of accounts.
        /// </summary>
        public const string ChartOfAccountsCacheKeyPrefix = "QuickBooksDataService.ChartOfAccounts";
    }

    /// <summary>
    ///     Rate limiting configuration for sync operations.
    /// </summary>
    public static class RateLimiting
    {
        /// <summary>
        ///     Maximum number of sync requests allowed per time window.
        /// </summary>
        public const int MaxRequestsPerWindow = 10;

        /// <summary>
        ///     Time window for rate limiting.
        /// </summary>
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     Cache key prefix for rate limiting data.
        /// </summary>
        public const string CacheKeyPrefix = "quickbooks:sync-rate:";
    }

    /// <summary>
    ///     Sync operation timeouts and windows.
    /// </summary>
    public static class SyncConfiguration
    {
        /// <summary>
        ///     Time window to consider a sync operation as "in progress" before allowing retry.
        /// </summary>
        public static readonly TimeSpan InProgressWindow = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     Polling interval for the background sync hosted service.
        /// </summary>
        public static readonly TimeSpan BackgroundSyncPollingInterval = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    ///     OAuth and token configuration.
    /// </summary>
    public static class OAuth
    {
        /// <summary>
        ///     QuickBooks accounting scope for API access.
        /// </summary>
        public const string AccountingScope = "com.intuit.quickbooks.accounting";

        /// <summary>
        ///     Data protection purpose string for encrypting tokens.
        /// </summary>
        public const string TokenProtectorPurpose = "Caskr.Server.Services.QuickBooksAuthService.Tokens";

        /// <summary>
        ///     Time skew to apply when checking token expiration (refresh before actual expiry).
        /// </summary>
        public static readonly TimeSpan TokenExpirySkew = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    ///     Allowed sync frequency values for accounting preferences.
    /// </summary>
    public static class SyncFrequencies
    {
        public const string Immediate = "Immediate";
        public const string Hourly = "Hourly";
        public const string Daily = "Daily";
        public const string Manual = "Manual";

        /// <summary>
        ///     Default sync frequency when none is specified.
        /// </summary>
        public const string Default = Manual;

        /// <summary>
        ///     All allowed sync frequency values.
        /// </summary>
        public static readonly string[] AllAllowed = { Immediate, Hourly, Daily, Manual };
    }

    /// <summary>
    ///     QuickBooks tax code values.
    /// </summary>
    public static class TaxCodes
    {
        public const string Taxable = "TAX";
        public const string NonTaxable = "NON";
    }

    /// <summary>
    ///     Configuration keys for appsettings.json.
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string ClientId = "QuickBooks:ClientId";
        public const string ClientSecret = "QuickBooks:ClientSecret";
        public const string RedirectUri = "QuickBooks:RedirectUri";
        public const string ConnectSuccessRedirectUrl = "QuickBooks:ConnectSuccessRedirectUrl";
        public const string Environment = "QuickBooks:Environment";

        public const string DefaultEnvironment = "sandbox";
        public const string DefaultConnectSuccessRedirectUrl = "/accounting/quickbooks/success";
    }
}
