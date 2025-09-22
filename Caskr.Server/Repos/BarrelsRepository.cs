using System;
using System.Collections.Generic;
using System.Linq;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IBarrelsRepository
    {
        Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId);
        Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears);
        Task<Dictionary<string, int>?> GetRickhouseIdsByNameAsync(int companyId, IEnumerable<string> normalizedNames);
        Task<bool> BatchExistsForCompanyAsync(int companyId, int batchId);
        Task<bool> MashBillExistsForCompanyAsync(int companyId, int mashBillId);
        Task<int> CreateBatchAsync(int companyId, int mashBillId);
        Task<int> EnsureOrderForBatchAsync(int companyId, int ownerId, int batchId, int quantity);
        Task AddBarrelsAsync(IEnumerable<Barrel> barrels);
    }

    public class BarrelsRepository(CaskrDbContext dbContext) : IBarrelsRepository
    {
        public async Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId)
        {
            return await dbContext.Barrels
                .Include(b => b.Order)
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears)
        {
            var cutoffDate = targetDate.AddYears(-ageYears);
            return await dbContext.Barrels
                .Include(b => b.Order)
                .Where(b => b.CompanyId == companyId && b.Order != null && b.Order.CreatedAt <= cutoffDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>?> GetRickhouseIdsByNameAsync(int companyId, IEnumerable<string> normalizedNames)
        {
            var normalizedSet = normalizedNames.Select(n => n.ToLowerInvariant()).ToHashSet();
            return await dbContext.Rickhouses
                .Where(r => r.CompanyId == companyId && normalizedSet.Contains(r.Name.ToLowerInvariant()))
                .ToDictionaryAsync(r => r.Name.ToLowerInvariant(), r => r.Id);
        }

        public Task<bool> BatchExistsForCompanyAsync(int companyId, int batchId)
        {
            return dbContext.Batches.AnyAsync(b => b.CompanyId == companyId && b.Id == batchId);
        }

        public Task<bool> MashBillExistsForCompanyAsync(int companyId, int mashBillId)
        {
            return dbContext.MashBills.AnyAsync(m => m.CompanyId == companyId && m.Id == mashBillId);
        }

        public async Task<int> CreateBatchAsync(int companyId, int mashBillId)
        {
            var maxExistingId = await dbContext.Batches
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.Id)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            var nextId = maxExistingId + 1;

            var batch = new Batch
            {
                Id = nextId,
                CompanyId = companyId,
                MashBillId = mashBillId
            };

            dbContext.Batches.Add(batch);
            await dbContext.SaveChangesAsync();
            return batch.Id;
        }

        public async Task<int> EnsureOrderForBatchAsync(int companyId, int ownerId, int batchId, int quantity)
        {
            var order = await dbContext.Orders
                .Where(o => o.CompanyId == companyId && o.BatchId == batchId)
                .OrderBy(o => o.Id)
                .FirstOrDefaultAsync();

            if (order is not null)
            {
                order.Quantity += quantity;
                order.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return order.Id;
            }

            var statusId = await dbContext.Statuses
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            var spiritTypeId = await dbContext.SpiritTypes
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (statusId == 0 || spiritTypeId == 0)
            {
                throw new InvalidOperationException("No status or spirit type records are available to create an order.");
            }

            var newOrder = new Order
            {
                Name = $"Imported Barrels {DateTime.UtcNow:yyyyMMddHHmmss}",
                OwnerId = ownerId,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StatusId = statusId,
                SpiritTypeId = spiritTypeId,
                BatchId = batchId,
                Quantity = quantity
            };

            dbContext.Orders.Add(newOrder);
            await dbContext.SaveChangesAsync();
            return newOrder.Id;
        }

        public async Task AddBarrelsAsync(IEnumerable<Barrel> barrels)
        {
            await dbContext.Barrels.AddRangeAsync(barrels);
            await dbContext.SaveChangesAsync();
        }
    }
}
