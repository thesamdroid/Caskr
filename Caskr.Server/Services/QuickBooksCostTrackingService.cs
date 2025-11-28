using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Caskr.server.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

[AutoBind]
public sealed class QuickBooksCostTrackingService : IQuickBooksCostTrackingService
{
    private static readonly HashSet<CaskrAccountType> CostAccountTypes = new()
    {
        CaskrAccountType.RawMaterials,
        CaskrAccountType.Barrels,
        CaskrAccountType.Labor,
        CaskrAccountType.Ingredients,
        CaskrAccountType.Overhead
    };

    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksIntegrationContextFactory _integrationContextFactory;
    private readonly IQuickBooksJournalEntryClient _journalEntryClient;
    private readonly IQuickBooksSyncLogService _syncLogService;
    private readonly IQuickBooksAccountMappingService _accountMappingService;
    private readonly ILogger<QuickBooksCostTrackingService> _logger;

    public QuickBooksCostTrackingService(
        CaskrDbContext dbContext,
        IQuickBooksIntegrationContextFactory integrationContextFactory,
        IQuickBooksJournalEntryClient journalEntryClient,
        IQuickBooksSyncLogService syncLogService,
        IQuickBooksAccountMappingService accountMappingService,
        ILogger<QuickBooksCostTrackingService> logger)
    {
        _dbContext = dbContext;
        _integrationContextFactory = integrationContextFactory;
        _journalEntryClient = journalEntryClient;
        _syncLogService = syncLogService;
        _accountMappingService = accountMappingService;
        _logger = logger;
    }

    public async Task<JournalEntrySyncResult> RecordBatchCOGSAsync(int batchId)
    {
        if (batchId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchId));
        }

        var batch = await _dbContext.Batches
            .AsNoTracking()
            .Include(b => b.MashBill)
            .SingleOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
        {
            _logger.LogWarning("Batch {BatchId} was not found. Skipping COGS sync.", batchId);
            return new JournalEntrySyncResult(false, null, $"Batch {batchId} was not found.");
        }

        var batchIdText = batchId.ToString(CultureInfo.InvariantCulture);
        var existingQboId = await _syncLogService.GetSuccessfulSyncExternalIdAsync(
            batch.CompanyId,
            QuickBooksConstants.EntityTypes.Batch,
            batchIdText);

        if (existingQboId is not null)
        {
            _logger.LogInformation(
                "Batch {BatchId} already synced to QuickBooks with journal entry {JournalEntryId}",
                batchId,
                existingQboId);
            return new JournalEntrySyncResult(true, existingQboId, null);
        }

        var syncLog = await _syncLogService.GetOrCreateSyncLogAsync(
            batch.CompanyId,
            QuickBooksConstants.EntityTypes.Batch,
            batchIdText);

        try
        {
            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.InProgress, null, syncLog.ExternalEntityId);
            await _dbContext.SaveChangesAsync();

            var orders = await LoadOrdersWithCostsAsync(batch.CompanyId, batchId);
            var lineItems = orders
                .Where(o => o.Invoice is not null)
                .SelectMany(o => o.Invoice!.LineItems)
                .Where(li => CostAccountTypes.Contains(li.AccountType))
                .ToList();

            if (lineItems.Count == 0)
            {
                var error = $"Batch {batchId} does not contain any cost components.";
                _logger.LogWarning(error);
                _syncLogService.IncrementRetryCount(syncLog);
                _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, error, syncLog.ExternalEntityId);
                await _dbContext.SaveChangesAsync();
                return new JournalEntrySyncResult(false, null, error);
            }

            var totalCost = CalculateTotalCost(lineItems);
            if (totalCost <= 0)
            {
                var error = $"Batch {batchId} total cost is zero. Nothing to sync.";
                _logger.LogWarning(error);
                _syncLogService.IncrementRetryCount(syncLog);
                _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, error, syncLog.ExternalEntityId);
                await _dbContext.SaveChangesAsync();
                return new JournalEntrySyncResult(false, null, error);
            }

            await _accountMappingService.ValidateRequiredMappingsExistAsync(
                batch.CompanyId,
                CaskrAccountType.Cogs,
                CaskrAccountType.WorkInProgress);

            var accountMappings = await _accountMappingService.LoadAccountMappingsAsync(batch.CompanyId);
            var cogsAccount = accountMappings[CaskrAccountType.Cogs];
            var wipAccount = accountMappings[CaskrAccountType.WorkInProgress];

            var integrationContext = await _integrationContextFactory.CreateAsync(batch.CompanyId);
            var serviceContext = integrationContext.ServiceContext;
            var description = BuildDescription(batch, orders);
            var journalEntry = BuildJournalEntry(totalCost, description, cogsAccount, wipAccount);
            var createdEntry = await _journalEntryClient.CreateJournalEntryAsync(serviceContext, journalEntry, CancellationToken.None);

            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Success, null, createdEntry.Id);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Recorded COGS for batch {BatchId} with QuickBooks journal entry {JournalEntryId}",
                batchId,
                createdEntry.Id);

            return new JournalEntrySyncResult(true, createdEntry.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record COGS for batch {BatchId}", batchId);
            _syncLogService.IncrementRetryCount(syncLog);
            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, ex.Message, syncLog.ExternalEntityId);
            await _dbContext.SaveChangesAsync();
            return new JournalEntrySyncResult(false, null, ex.Message);
        }
    }

    private static decimal CalculateTotalCost(IEnumerable<InvoiceLineItem> lineItems)
    {
        var total = 0m;
        foreach (var item in lineItems)
        {
            var amount = Math.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            total += amount;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<List<Order>> LoadOrdersWithCostsAsync(int companyId, int batchId)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.BatchId == batchId)
            .Include(o => o.Invoice!)
                .ThenInclude(i => i.LineItems)
            .ToListAsync();
    }

    private static string BuildDescription(Batch batch, IReadOnlyCollection<Order> orders)
    {
        var firstOrderName = orders
            .Select(o => o.Name)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        var mashBillName = batch.MashBill?.Name;
        var descriptor = firstOrderName
            ?? mashBillName
            ?? $"Company {batch.CompanyId}";

        return $"Batch {batch.Id} completion - {descriptor}";
    }

    private static JournalEntry BuildJournalEntry(
        decimal totalCost,
        string description,
        ChartOfAccountsMapping cogsAccount,
        ChartOfAccountsMapping wipAccount)
    {
        var debitLine = CreateJournalLine(description, totalCost, cogsAccount, PostingTypeEnum.Debit);
        var creditLine = CreateJournalLine(description, totalCost, wipAccount, PostingTypeEnum.Credit);

        return new JournalEntry
        {
            DocNumber = $"BATCH-{Guid.NewGuid():N}".ToUpperInvariant(),
            PrivateNote = description,
            TxnDate = DateTime.UtcNow.Date,
            TxnDateSpecified = true,
            Line = new[] { debitLine, creditLine }
        };
    }

    private static Line CreateJournalLine(
        string description,
        decimal amount,
        ChartOfAccountsMapping account,
        PostingTypeEnum postingType)
    {
        return new Line
        {
            Description = description,
            Amount = amount,
            DetailType = LineDetailTypeEnum.JournalEntryLineDetail,
            AnyIntuitObject = new JournalEntryLineDetail
            {
                PostingType = postingType,
                PostingTypeSpecified = true,
                AccountRef = new ReferenceType
                {
                    Value = account.QboAccountId,
                    name = account.QboAccountName
                }
            }
        };
    }
}
