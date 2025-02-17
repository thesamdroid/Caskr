using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IOrdersService
    {
        Task<IEnumerable<Order?>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(int id);
        Task AddOrderAsync(Order? order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
    }

    public class OrdersService(IOrdersRepository ordersRepository) : IOrdersService
    {
        public async Task<IEnumerable<Order?>> GetOrdersAsync()
        {
            return await ordersRepository.GetOrdersAsync();
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await ordersRepository.GetOrderAsync(id);
        }   

        public async Task AddOrderAsync(Order? order)
        {
            await ordersRepository.AddOrderAsync(order);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            await ordersRepository.UpdateOrderAsync(order);
        }

        public async Task DeleteOrderAsync(int id)
        {
            await ordersRepository.DeleteOrderAsync(id);
        }   
    }
}
