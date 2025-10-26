using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Caskr.server.Models
{
    /// <summary>
    /// Represents a task associated with an order
    /// </summary>
    public class OrderTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int OrderId { get; set; }

        /// <summary>
        /// User assigned to this task
        /// </summary>
        public int? AssigneeId { get; set; }

        /// <summary>
        /// Whether the task has been completed
        /// </summary>
        public bool IsComplete { get; set; } = false;

        /// <summary>
        /// Due date for the task (optional)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// When the task was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the task was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the task was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("AssigneeId")]
        public virtual User? Assignee { get; set; }
    }
}
