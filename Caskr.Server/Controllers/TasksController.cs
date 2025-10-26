using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : AuthorizedApiControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>
        /// Get all tasks for a specific order
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderTask>>> GetTasksByOrder(int orderId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByOrderIdAsync(orderId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch tasks for order {OrderId}", orderId);
                return StatusCode(500, new { message = "Failed to fetch tasks" });
            }
        }

        /// <summary>
        /// Compatibility endpoint for clients requesting tasks via the orders route.
        /// </summary>
        [HttpGet("~/api/orders/{orderId}/tasks")]
        public Task<ActionResult<IEnumerable<OrderTask>>> GetTasksByOrderRouteAlias(int orderId)
        {
            return GetTasksByOrder(orderId);
        }

        /// <summary>
        /// Assign a task to a user
        /// </summary>
        [HttpPut("{taskId}/assign")]
        public async Task<ActionResult<OrderTask>> AssignTask(
            int taskId,
            [FromBody] AssignTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found" });
                }

                var updatedTask = await _taskService.AssignTaskAsync(taskId, request.AssigneeId);

                _logger.LogInformation(
                    "Task {TaskId} assigned to user {UserId}",
                    taskId,
                    request.AssigneeId);

                return Ok(updatedTask);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid task assignment");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign task {TaskId}", taskId);
                return StatusCode(500, new { message = "Failed to assign task" });
            }
        }

        /// <summary>
        /// Mark a task as complete or incomplete
        /// </summary>
        [HttpPut("{taskId}/complete")]
        public async Task<ActionResult<OrderTask>> CompleteTask(
            int taskId,
            [FromBody] CompleteTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found" });
                }

                var updatedTask = await _taskService.CompleteTaskAsync(taskId, request.IsComplete);

                _logger.LogInformation(
                    "Task {TaskId} marked as {Status}",
                    taskId,
                    request.IsComplete ? "complete" : "incomplete");

                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update task {TaskId}", taskId);
                return StatusCode(500, new { message = "Failed to update task" });
            }
        }

        /// <summary>
        /// Create a new task for an order
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrderTask>> CreateTask([FromBody] CreateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                var task = await _taskService.CreateTaskAsync(
                    request.OrderId,
                    request.Name,
                    request.AssigneeId,
                    request.DueDate);

                _logger.LogInformation(
                    "Task created: {TaskName} for order {OrderId}",
                    task.Name,
                    task.OrderId);

                return CreatedAtAction(
                    nameof(GetTasksByOrder),
                    new { orderId = task.OrderId },
                    task);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid task creation");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task");
                return StatusCode(500, new { message = "Failed to create task" });
            }
        }

        /// <summary>
        /// Delete a task
        /// </summary>
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found" });
                }

                await _taskService.DeleteTaskAsync(taskId);

                _logger.LogInformation("Task {TaskId} deleted", taskId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task {TaskId}", taskId);
                return StatusCode(500, new { message = "Failed to delete task" });
            }
        }
    }

    #region Request Models

    public class AssignTaskRequest
    {
        [Required]
        public int? AssigneeId { get; set; }
    }

    public class CompleteTaskRequest
    {
        [Required]
        public bool IsComplete { get; set; }
    }

    public class CreateTaskRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public int? AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }
    }

    #endregion
}
