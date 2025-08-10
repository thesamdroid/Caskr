using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;

namespace Caskr.server.Repos
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(int id);
        Task<Order?> GetOrderWithTasksAsync(int id);
        Task<Order> AddOrderAsync(Order? order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
    }

    public class OrdersRepository(CaskrDbContext dbContext) : IOrdersRepository
    {
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return (await dbContext.Orders.ToListAsync())!;
        }
        public async Task<Order?> GetOrderAsync(int id)
        {
            return await dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        }
        public async Task<Order?> GetOrderWithTasksAsync(int id)
        {
            return await dbContext.Orders
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
