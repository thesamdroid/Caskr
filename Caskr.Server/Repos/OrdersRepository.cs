using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Caskr.server.Repos
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(int id);
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
        public async Task<Order> AddOrderAsync(Order? order)
        {
            var addedOrder = await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();
            return addedOrder.Entity!;
        }
        public async Task<Order> UpdateOrderAsync(Order order)
        {
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
