using System.Linq;
using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IOrdersService
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order> AddOrderAsync(Order? order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
        Task<IEnumerable<StatusTask>?> GetOutstandingTasksAsync(int orderId);
    }

    public class OrdersService(IOrdersRepository ordersRepository, IUsersRepository usersRepository, IEmailService emailService) : IOrdersService
    {
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await ordersRepository.GetOrdersAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId)
        {
            return await ordersRepository.GetOrdersForOwnerAsync(ownerId);
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await ordersRepository.GetOrderByIdAsync(id);
        }
        public async Task<IEnumerable<StatusTask>?> GetOutstandingTasksAsync(int orderId)
        {
            var order = await ordersRepository.GetOrderWithTasksAsync(orderId);
            if (order == null)
            {
                return null;
            }
            var completed = order.Tasks.Select(t => t.Name).ToHashSet();
            return order.Status.StatusTasks
                .Where(st => !completed.Contains(st.Name))
                .Select(st => new StatusTask
                {
                    Id = st.Id,
                    StatusId = st.StatusId,
                    Name = st.Name
                });
        }


        public async Task<Order> AddOrderAsync(Order? order)
        {
            return await ordersRepository.AddOrderAsync(order);
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var existingOrder = await ordersRepository.GetOrderByIdAsync(order.Id);
            var updated = await ordersRepository.UpdateOrderAsync(order);
            if (existingOrder?.StatusId != updated.StatusId)
            {
                await ordersRepository.AddTasksForStatusAsync(updated.Id, updated.StatusId);
            }
            if (existingOrder?.StatusId != (int)StatusType.TtbApproval && updated.StatusId == (int)StatusType.TtbApproval)
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
