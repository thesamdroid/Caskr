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
using Intuit.Ipp.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

[AutoBind]
public sealed class QuickBooksCostTrackingService : IQuickBooksCostTrackingService
{
    private const string BatchEntityType = "Batch";
    private static readonly HashSet<CaskrAccountType> CostAccountTypes = new()
    {
        CaskrAccountType.RawMaterials,
        CaskrAccountType.Barrels,
        CaskrAccountType.Labor,
        CaskrAccountType.Ingredients,
        CaskrAccountType.Overhead
    };

    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksAuthService _authService;
    private readonly IQuickBooksJournalEntryClient _journalEntryClient;
    private readonly ILogger<QuickBooksCostTrackingService> _logger;

    public QuickBooksCostTrackingService(
        CaskrDbContext dbContext,
        IQuickBooksAuthService authService,
        IQuickBooksJournalEntryClient journalEntryClient,
        ILogger<QuickBooksCostTrackingService> logger)
    {
        _dbContext = dbContext;
        _authService = authService;
        _journalEntryClient = journalEntryClient;
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
        var existingSuccess = await _dbContext.AccountingSyncLogs
            .AsNoTracking()
            .Where(log => log.CompanyId == batch.CompanyId
                          && log.EntityType == BatchEntityType
                          && log.EntityId == batchIdText
                          && log.SyncStatus == SyncStatus.Success)
            .OrderByDescending(log => log.SyncedAt)
            .FirstOrDefaultAsync();

        if (existingSuccess is not null)
        {
            _logger.LogInformation(
                "Batch {BatchId} already synced to QuickBooks with journal entry {JournalEntryId}",
                batchId,
                existingSuccess.ExternalEntityId);
            return new JournalEntrySyncResult(true, existingSuccess.ExternalEntityId, null);
        }

        var syncLog = await GetOrCreateSyncLogAsync(batch.CompanyId, batchId);

        try
        {
            UpdateSyncLog(syncLog, SyncStatus.InProgress, null, syncLog.ExternalEntityId);
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
                syncLog.RetryCount += 1;
                UpdateSyncLog(syncLog, SyncStatus.Failed, error, syncLog.ExternalEntityId);
                await _dbContext.SaveChangesAsync();
                return new JournalEntrySyncResult(false, null, error);
            }

            var totalCost = CalculateTotalCost(lineItems);
            if (totalCost <= 0)
            {
                var error = $"Batch {batchId} total cost is zero. Nothing to sync.";
                _logger.LogWarning(error);
                syncLog.RetryCount += 1;
                UpdateSyncLog(syncLog, SyncStatus.Failed, error, syncLog.ExternalEntityId);
                await _dbContext.SaveChangesAsync();
                return new JournalEntrySyncResult(false, null, error);
            }

            var accountMappings = await LoadAccountMappingsAsync(batch.CompanyId);
            if (!accountMappings.TryGetValue(CaskrAccountType.Cogs, out var cogsAccount))
            {
                throw new InvalidOperationException("COGS account mapping is missing.");
            }

            if (!accountMappings.TryGetValue(CaskrAccountType.WorkInProgress, out var wipAccount))
            {
                throw new InvalidOperationException("Work In Progress account mapping is missing.");
            }

            var serviceContext = await CreateServiceContextAsync(batch.CompanyId);
            var description = BuildDescription(batch, orders);
            var journalEntry = BuildJournalEntry(totalCost, description, cogsAccount, wipAccount);
            var createdEntry = await _journalEntryClient.CreateJournalEntryAsync(serviceContext, journalEntry, CancellationToken.None);

            UpdateSyncLog(syncLog, SyncStatus.Success, null, createdEntry.Id);
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
            syncLog.RetryCount += 1;
            UpdateSyncLog(syncLog, SyncStatus.Failed, ex.Message, syncLog.ExternalEntityId);
            await _dbContext.SaveChangesAsync();
            return new JournalEntrySyncResult(false, null, ex.Message);
        }
    }

    private async Task<AccountingSyncLog> GetOrCreateSyncLogAsync(int companyId, int batchId)
    {
        var batchIdText = batchId.ToString(CultureInfo.InvariantCulture);
        var log = await _dbContext.AccountingSyncLogs
            .SingleOrDefaultAsync(l => l.CompanyId == companyId
                                       && l.EntityType == BatchEntityType
                                       && l.EntityId == batchIdText);

        if (log is not null)
        {
            return log;
        }

        log = new AccountingSyncLog
        {
            CompanyId = companyId,
            EntityType = BatchEntityType,
            EntityId = batchIdText,
            SyncStatus = SyncStatus.Pending,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.AccountingSyncLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        return log;
    }

    private static void UpdateSyncLog(AccountingSyncLog log, SyncStatus status, string? errorMessage, string? externalId)
    {
        log.SyncStatus = status;
        log.ErrorMessage = errorMessage;
        log.ExternalEntityId = externalId;
        log.SyncedAt = DateTime.UtcNow;
        log.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<ServiceContext> CreateServiceContextAsync(int companyId)
    {
        var integration = await _dbContext.AccountingIntegrations
            .AsNoTracking()
            .SingleOrDefaultAsync(ai => ai.CompanyId == companyId
                                        && ai.Provider == AccountingProvider.QuickBooks
                                        && ai.IsActive);

        if (integration is null)
        {
            throw new InvalidOperationException($"Company {companyId} does not have an active QuickBooks integration.");
        }

        var tokenResponse = await _authService.RefreshTokenAsync(companyId);
        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("QuickBooks access token is missing.");
        }

        var realmId = !string.IsNullOrWhiteSpace(tokenResponse.RealmId)
            ? tokenResponse.RealmId
            : integration.RealmId;

        if (string.IsNullOrWhiteSpace(realmId))
        {
            throw new InvalidOperationException("QuickBooks realm ID is missing.");
        }

        var validator = new OAuth2RequestValidator(tokenResponse.AccessToken);
        return new ServiceContext(realmId, IntuitServicesType.QBO, validator);
    }

    private async Task<Dictionary<CaskrAccountType, ChartOfAccountsMapping>> LoadAccountMappingsAsync(int companyId)
    {
        var mappings = await _dbContext.ChartOfAccountsMappings
            .AsNoTracking()
            .Where(m => m.CompanyId == companyId)
            .ToListAsync();

        return mappings.ToDictionary(m => m.CaskrAccountType, m => m);
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
