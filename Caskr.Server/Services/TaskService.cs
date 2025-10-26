using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<OrderTask>> GetTasksByOrderIdAsync(int orderId);
        Task<OrderTask?> GetTaskByIdAsync(int taskId);
        Task<OrderTask> CreateTaskAsync(int orderId, string name, int? assigneeId, DateTime? dueDate);
        Task<OrderTask> AssignTaskAsync(int taskId, int? assigneeId);
        Task<OrderTask> CompleteTaskAsync(int taskId, bool isComplete);
        Task DeleteTaskAsync(int taskId);
    }

    public class TaskService : ITaskService
    {
        private readonly CaskrDbContext _dbContext;
        private readonly ILogger<TaskService> _logger;

        public TaskService(CaskrDbContext dbContext, ILogger<TaskService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<OrderTask>> GetTasksByOrderIdAsync(int orderId)
        {
            return await _dbContext.Tasks
                .Where(t => t.OrderId == orderId)
                .Include(t => t.Assignee)
                .OrderBy(t => t.IsComplete)
                .ThenBy(t => t.DueDate)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<OrderTask?> GetTaskByIdAsync(int taskId)
        {
            return await _dbContext.Tasks
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<OrderTask> CreateTaskAsync(
            int orderId,
            string name,
            int? assigneeId,
            DateTime? dueDate)
        {
            // Validate order exists
            var orderExists = await _dbContext.Orders.AnyAsync(o => o.Id == orderId);
            if (!orderExists)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            // Validate assignee if provided
            if (assigneeId.HasValue)
            {
                var assigneeExists = await _dbContext.Users.AnyAsync(u => u.Id == assigneeId.Value);
                if (!assigneeExists)
                {
                    throw new ArgumentException($"User with ID {assigneeId} not found");
                }
            }

            var task = new OrderTask
            {
                OrderId = orderId,
                Name = name.Trim(),
                AssigneeId = assigneeId,
                DueDate = dueDate,
                IsComplete = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created task {TaskName} for order {OrderId}",
                task.Name,
                task.OrderId);

            return task;
        }

        public async Task<OrderTask> AssignTaskAsync(int taskId, int? assigneeId)
        {
            var task = await _dbContext.Tasks
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                throw new ArgumentException($"Task with ID {taskId} not found");
            }

            // Validate assignee if provided
            if (assigneeId.HasValue)
            {
                var assigneeExists = await _dbContext.Users.AnyAsync(u => u.Id == assigneeId.Value);
                if (!assigneeExists)
                {
                    throw new ArgumentException($"User with ID {assigneeId} not found");
                }
            }

            var previousAssigneeId = task.AssigneeId;
            task.AssigneeId = assigneeId;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Task {TaskId} reassigned from user {PreviousAssigneeId} to user {NewAssigneeId}",
                taskId,
                previousAssigneeId,
                assigneeId);

            return task;
        }

        public async Task<OrderTask> CompleteTaskAsync(int taskId, bool isComplete)
        {
            var task = await _dbContext.Tasks
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                throw new ArgumentException($"Task with ID {taskId} not found");
            }

            task.IsComplete = isComplete;
            task.CompletedAt = isComplete ? DateTime.UtcNow : null;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Task {TaskId} marked as {Status}",
                taskId,
                isComplete ? "complete" : "incomplete");

            return task;
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);

            if (task == null)
            {
                throw new ArgumentException($"Task with ID {taskId} not found");
            }

            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted task {TaskId}", taskId);
        }
    }
}
