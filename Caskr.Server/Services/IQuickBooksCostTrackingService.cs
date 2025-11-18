using System.Threading.Tasks;

namespace Caskr.Server.Services;

/// <summary>
///     Records cost of goods sold journal entries in QuickBooks when batches are completed.
/// </summary>
public interface IQuickBooksCostTrackingService
{
    Task<JournalEntrySyncResult> RecordBatchCOGSAsync(int batchId);
}
