using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Provides centralized management of accounting sync logs to eliminate duplication
///     and ensure consistent handling across all QuickBooks sync operations.
/// </summary>
public interface IQuickBooksSyncLogService
{
    /// <summary>
    ///     Retrieves or creates a sync log entry for the specified entity.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="entityType">The type of entity being synced (Invoice, Batch, etc.).</param>
    /// <param name="entityId">The identifier of the entity being synced.</param>
    /// <returns>The sync log entry.</returns>
    Task<AccountingSyncLog> GetOrCreateSyncLogAsync(int companyId, string entityType, string entityId);

    /// <summary>
    ///     Updates an existing sync log with new status information.
    /// </summary>
    /// <param name="log">The sync log to update.</param>
    /// <param name="status">The new sync status.</param>
    /// <param name="errorMessage">Optional error message if the sync failed.</param>
    /// <param name="externalEntityId">Optional QuickBooks entity identifier if the sync succeeded.</param>
    void UpdateSyncLog(AccountingSyncLog log, SyncStatus status, string? errorMessage, string? externalEntityId);

    /// <summary>
    ///     Checks if an entity has already been successfully synced to QuickBooks.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="entityType">The type of entity being synced.</param>
    /// <param name="entityId">The identifier of the entity being synced.</param>
    /// <returns>The external entity ID if already synced, otherwise null.</returns>
    Task<string?> GetSuccessfulSyncExternalIdAsync(int companyId, string entityType, string entityId);

    /// <summary>
    ///     Increments the retry count for a sync log entry.
    /// </summary>
    /// <param name="log">The sync log entry.</param>
    void IncrementRetryCount(AccountingSyncLog log);
}

/// <summary>
///     Implementation of sync log management service.
/// </summary>
[AutoBind]
public sealed class QuickBooksSyncLogService : IQuickBooksSyncLogService
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<QuickBooksSyncLogService> _logger;

    public QuickBooksSyncLogService(
        CaskrDbContext dbContext,
        ILogger<QuickBooksSyncLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AccountingSyncLog> GetOrCreateSyncLogAsync(int companyId, string entityType, string entityId)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        var log = await _dbContext.AccountingSyncLogs
            .SingleOrDefaultAsync(l => l.CompanyId == companyId
                                       && l.EntityType == entityType
                                       && l.EntityId == entityId);

        if (log is not null)
        {
            return log;
        }

        _logger.LogInformation(
            "Creating new sync log for company {CompanyId}, entity type {EntityType}, entity ID {EntityId}",
            companyId,
            entityType,
            entityId);

        log = new AccountingSyncLog
        {
            CompanyId = companyId,
            EntityType = entityType,
            EntityId = entityId,
            SyncStatus = SyncStatus.Pending,
            RetryCount = 0,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.AccountingSyncLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        return log;
    }

    public void UpdateSyncLog(AccountingSyncLog log, SyncStatus status, string? errorMessage, string? externalEntityId)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        log.SyncStatus = status;
        log.ErrorMessage = errorMessage;
        log.ExternalEntityId = externalEntityId;
        log.SyncedAt = DateTime.UtcNow;
        log.UpdatedAt = DateTime.UtcNow;

        _logger.LogDebug(
            "Updated sync log for {EntityType} {EntityId}: status={Status}, externalId={ExternalId}",
            log.EntityType,
            log.EntityId,
            status,
            externalEntityId);
    }

    public async Task<string?> GetSuccessfulSyncExternalIdAsync(int companyId, string entityType, string entityId)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        var existingSuccess = await _dbContext.AccountingSyncLogs
            .AsNoTracking()
            .Where(log => log.CompanyId == companyId
                          && log.EntityType == entityType
                          && log.EntityId == entityId
                          && log.SyncStatus == SyncStatus.Success)
            .OrderByDescending(log => log.SyncedAt)
            .FirstOrDefaultAsync();

        return existingSuccess?.ExternalEntityId;
    }

    public void IncrementRetryCount(AccountingSyncLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        log.RetryCount += 1;
    }
}
