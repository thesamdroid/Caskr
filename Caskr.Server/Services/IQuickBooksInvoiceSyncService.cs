using System.Threading.Tasks;

namespace Caskr.Server.Services;

/// <summary>
///     Defines the operations required to synchronize invoices and payments from Caskr to QuickBooks Online.
///     Implementations should be invoked immediately after an invoice is created as well as by the nightly batch job.
///     The sync is intentionally unidirectional (Caskr → QBO); bidirectional reconciliation will be handled in a future
///     enhancement when QuickBooks webhooks and delta queries are introduced.
/// </summary>
public interface IQuickBooksInvoiceSyncService
{
    /// <summary>
    ///     Syncs a single invoice into QuickBooks Online. Implementations must ensure the customer exists (creating one when
    ///     needed), push the invoice payload to QBO, and record the attempt in <c>accounting_sync_logs</c>.
    /// </summary>
    /// <param name="invoiceId">The internal invoice identifier that should be synced.</param>
    /// <returns>Details about the sync attempt, including the resulting QBO invoice identifier.</returns>
    Task<InvoiceSyncResult> SyncInvoiceToQBOAsync(int invoiceId);

    /// <summary>
    ///     Syncs the payment associated with the supplied payment record into QuickBooks Online.
    /// </summary>
    /// <param name="paymentId">The internal payment identifier that should be synced.</param>
    /// <returns>Information about the payment sync status and any QBO identifiers that were created.</returns>
    Task<PaymentSyncResult> SyncPaymentToQBOAsync(int paymentId);

    /// <summary>
    ///     Performs a batch sync for all invoices belonging to the specified company whose <c>sync_status</c> is Pending.
    ///     This method is typically executed by a nightly job to backfill any invoices that failed to sync immediately.
    /// </summary>
    /// <param name="companyId">The company whose pending invoices should be synchronized.</param>
    /// <returns>Aggregated statistics about the batch run, including counts of successes and failures.</returns>
    Task<BatchSyncResult> SyncAllPendingAsync(int companyId);
}
