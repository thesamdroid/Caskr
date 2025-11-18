using System.Collections.Generic;

namespace Caskr.Server.Services;

/// <summary>
///     Result of synchronizing a single Caskr invoice with QuickBooks Online.
/// </summary>
public sealed record InvoiceSyncResult
{
    public InvoiceSyncResult(bool success, string? qboInvoiceId, string? errorMessage)
    {
        Success = success;
        QboInvoiceId = qboInvoiceId;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Indicates whether the sync operation completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     The QuickBooks Online invoice identifier created during the sync, if any.
    /// </summary>
    public string? QboInvoiceId { get; }

    /// <summary>
    ///     When <see cref="Success" /> is false, contains details about the failure returned by QuickBooks or Caskr.
    /// </summary>
    public string? ErrorMessage { get; }
}

/// <summary>
///     Result of synchronizing a payment captured in Caskr with QuickBooks Online.
/// </summary>
public sealed record PaymentSyncResult
{
    public PaymentSyncResult(bool success, string? qboPaymentId, string? errorMessage)
    {
        Success = success;
        QboPaymentId = qboPaymentId;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Indicates whether the payment sync operation completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     The QuickBooks Online payment identifier that was created or updated as part of the sync.
    /// </summary>
    public string? QboPaymentId { get; }

    /// <summary>
    ///     When <see cref="Success" /> is false, contains details about the failure returned by QuickBooks or Caskr.
    /// </summary>
    public string? ErrorMessage { get; }
}

/// <summary>
///     Represents the result of syncing a batch cost of goods sold journal entry to QuickBooks.
/// </summary>
public sealed record JournalEntrySyncResult
{
    public JournalEntrySyncResult(bool success, string? qboJournalEntryId, string? errorMessage)
    {
        Success = success;
        QboJournalEntryId = qboJournalEntryId;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Indicates whether the journal entry was recorded successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     Identifier of the QuickBooks journal entry that was created, when successful.
    /// </summary>
    public string? QboJournalEntryId { get; }

    /// <summary>
    ///     Error description when the sync failed.
    /// </summary>
    public string? ErrorMessage { get; }
}

/// <summary>
///     Aggregates the results of a batch synchronization run, typically executed by the nightly job.
/// </summary>
public sealed record BatchSyncResult
{
    public BatchSyncResult(int successCount, int failureCount, IReadOnlyCollection<string>? errors = null)
    {
        SuccessCount = successCount;
        FailureCount = failureCount;
        Errors = errors ?? System.Array.Empty<string>();
    }

    /// <summary>
    ///     Number of invoices or payments that were synced successfully.
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    ///     Number of invoices or payments that failed to sync.
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    ///     Optional list of descriptive errors encountered during the batch run.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }
}
