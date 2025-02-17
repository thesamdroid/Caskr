using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Order?>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(int id);
        Task AddOrderAsync(Order? order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
    }

    public class OrdersRepository(CaskrDbContext dbContext) : IOrdersRepository
    {
        public async Task<IEnumerable<Order?>> GetOrdersAsync()
        {
            return await dbContext.Orders.ToListAsync();
        }
        public async Task<Order?> GetOrderAsync(int id)
        {
            return await dbContext.Orders.FindAsync(id);
        }
        public async Task AddOrderAsync(Order? order)
        {
            await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();
        }
        public async Task UpdateOrderAsync(Order order)
        {
            dbContext.Entry(order).State = EntityState.Modified;
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
