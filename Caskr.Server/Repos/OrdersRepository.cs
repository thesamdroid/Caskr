using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;

namespace Caskr.server.Repos
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersForCompanyAsync(int companyId);
        Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId);
        Task<Order?> GetOrderAsync(int id);
        Task<Order?> GetOrderWithTasksAsync(int id);
        Task<Order> AddOrderAsync(Order? order);
        Task<Order> UpdateOrderAsync(Order order);
        Task AddTasksForStatusAsync(int orderId, int statusId);
        Task DeleteOrderAsync(int id);
    }

    public class OrdersRepository(CaskrDbContext dbContext) : IOrdersRepository
    {
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await dbContext.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.SpiritType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersForCompanyAsync(int companyId)
        {
            return await dbContext.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.SpiritType)
                .Where(o => o.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId)
        {
            return await dbContext.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.SpiritType)
                .Where(o => o.OwnerId == ownerId)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await dbContext.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.SpiritType)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
        public async Task<Order?> GetOrderWithTasksAsync(int id)
        {
            return await dbContext.Orders
                .Include(o => o.SpiritType)
                .Include(o => o.Status)
                    .ThenInclude(s => s.StatusTasks)
                .Include(o => o.Tasks)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == id);
        }
        public async Task<Order> AddOrderAsync(Order? order)
        {
            if (order is null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            var owner = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == order.OwnerId);
            if (owner == null)
            {
                throw new InvalidOperationException($"Owner with ID {order.OwnerId} not found");
            }

            var maxBatch = await dbContext.Batches
                .Where(b => b.CompanyId == owner.CompanyId)
                .MaxAsync(b => (int?)b.Id) ?? 0;

            var batch = new Batch
            {
                Id = maxBatch + 1,
                CompanyId = owner.CompanyId,
                MashBillId = order.MashBillId
            };

            await dbContext.Batches.AddAsync(batch);

            order.BatchId = batch.Id;
            order.CompanyId = owner.CompanyId;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            var addedOrder = await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();
            return addedOrder.Entity!;
        }
        public async Task<Order> UpdateOrderAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            dbContext.Entry(order).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetOrderAsync(order.Id))!;
        }
        public async Task AddTasksForStatusAsync(int orderId, int statusId)
        {
            var existingTaskNames = await dbContext.Tasks
                .Where(t => t.OrderId == orderId)
                .Select(t => t.Name)
                .ToListAsync();

            var statusTasks = await dbContext.StatusTasks
                .Where(st => st.StatusId == statusId && !existingTaskNames.Contains(st.Name))
                .ToListAsync();

            if (statusTasks.Count == 0)
            {
                return;
            }

            var tasks = statusTasks.Select(st => new TaskItem
            {
                OrderId = orderId,
                Name = st.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await dbContext.Tasks.AddRangeAsync(tasks);
            await dbContext.SaveChangesAsync();
        }
        public async Task DeleteOrderAsync(int id)
        {
            var order = await dbContext.Orders.FindAsync(id);
            if (order != null)
            {
                dbContext.Orders.Remove(order);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
