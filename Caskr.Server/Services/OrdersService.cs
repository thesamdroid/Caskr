using System;
using System.Linq;
using System.Threading;
using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server;
using Caskr.Server.Events;
using MediatR;

namespace Caskr.server.Services
{
    public interface IOrdersService
    {
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId);
        Task<Order?> GetOrderAsync(int id);
        Task<Order> AddOrderAsync(Order? order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int id);
        Task<IEnumerable<StatusTask>?> GetOutstandingTasksAsync(int orderId);
    }

    public class OrdersService(
        IOrdersRepository ordersRepository,
        IUsersRepository usersRepository,
        IEmailService emailService,
        IMediator mediator) : IOrdersService
    {
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await ordersRepository.GetOrdersAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersForOwnerAsync(int ownerId)
        {
            var user = await usersRepository.GetUserByIdAsync(ownerId);
            if (user != null && user.UserTypeId == (int)UserType.Admin)
            {
                return await ordersRepository.GetOrdersForCompanyAsync(user.CompanyId);
            }

            return await ordersRepository.GetOrdersForOwnerAsync(ownerId);
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await ordersRepository.GetOrderAsync(id);
        }
        public async Task<IEnumerable<StatusTask>?> GetOutstandingTasksAsync(int orderId)
        {
            var order = await ordersRepository.GetOrderWithTasksAsync(orderId);
            if (order == null)
            {
                return null;
            }

            var completedTasks = (order.Tasks ?? Enumerable.Empty<OrderTask>())
                .Where(t => t.CompletedAt != null)
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var statusTasks = (order.Status?.StatusTasks ?? Enumerable.Empty<StatusTask>())
                .Where(st => !string.IsNullOrWhiteSpace(st.Name))
                .ToList();

            if (statusTasks.Count == 0)
            {
                return Enumerable.Empty<StatusTask>();
            }

            return statusTasks
                .Where(st => !completedTasks.Contains(st.Name))
                .Select(st => new StatusTask
                {
                    Id = st.Id,
                    StatusId = st.StatusId,
                    Name = st.Name
                })
                .ToList();
        }


        public async Task<Order> AddOrderAsync(Order? order)
        {
            return await ordersRepository.AddOrderAsync(order);
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var existing = await ordersRepository.GetOrderAsync(order.Id);
            var updated = await ordersRepository.UpdateOrderAsync(order);
            if (existing?.StatusId != updated.StatusId)
            {
                await ordersRepository.AddTasksForStatusAsync(updated.Id, updated.StatusId);
            }
            if (existing?.StatusId != (int)StatusType.TtbApproval && updated.StatusId == (int)StatusType.TtbApproval)
            {
                var user = await usersRepository.GetUserByIdAsync(updated.OwnerId);
                if (user != null)
                {
                    await emailService.SendEmailAsync(user.Email, "Order requires TTB approval", $"Order '{updated.Name}' has moved to TTB Approval.");
                }
            }

            if (!IsCompletedStatus(existing) && IsCompletedStatus(updated) && updated.InvoiceId.HasValue)
            {
                var companyId = updated.CompanyId != 0
                    ? updated.CompanyId
                    : existing?.CompanyId ?? 0;

                if (companyId != 0)
                {
                    await mediator.Publish(new OrderCompletedEvent(updated.Id, companyId, updated.InvoiceId), CancellationToken.None);
                }
            }
            return updated;
        }

        public async Task DeleteOrderAsync(int id)
        {
            await ordersRepository.DeleteOrderAsync(id);
        }   
        private static bool IsCompletedStatus(Order? order)
        {
            if (order?.Status?.Name == null)
            {
                return false;
            }

            return order.Status.Name.Contains("complete", StringComparison.OrdinalIgnoreCase);
        }
    }
}
