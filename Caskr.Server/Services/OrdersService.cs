using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IOrdersService
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(int id);
        Task<Order> AddOrderAsync(Order? order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
    }

    public class OrdersService(IOrdersRepository ordersRepository, IUsersRepository usersRepository, IEmailService emailService) : IOrdersService
    {
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await ordersRepository.GetOrdersAsync();
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await ordersRepository.GetOrderAsync(id);
        }   

        public async Task<Order> AddOrderAsync(Order? order)
        {
            return await ordersRepository.AddOrderAsync(order);
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var existing = await ordersRepository.GetOrderAsync(order.Id);
            var updated = await ordersRepository.UpdateOrderAsync(order);
            if (existing?.StatusId != StatusType.TtbApproval && updated.StatusId == StatusType.TtbApproval)
            {
                var user = await usersRepository.GetUserAsync(updated.OwnerId);
                if (user != null)
                {
                    await emailService.SendEmailAsync(user.Email, "Order requires TTB approval", $"Order '{updated.Name}' has moved to TTB Approval.");
                }
            }
            return updated;
        }

        public async Task DeleteOrderAsync(int id)
        {
            await ordersRepository.DeleteOrderAsync(id);
        }   
    }
}
