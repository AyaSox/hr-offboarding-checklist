using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public class ChecklistItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        [StringLength(500)]
        public string? Comments { get; set; }

        [StringLength(100)]
        public string? CompletedBy { get; set; }

        public DateTime? CompletedOn { get; set; }
        public DateTime? DueDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[0];

        // Navigation properties
        public int OffboardingProcessId { get; set; }
        public OffboardingProcess OffboardingProcess { get; set; } = null!;

        public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

        // Computed properties
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date && !IsCompleted;

        // Task Dependencies
        public int? DependsOnTaskId { get; set; }
        public ChecklistItem? DependsOnTask { get; set; }
        public ICollection<ChecklistItem> DependentTasks { get; set; } = new List<ChecklistItem>();

        public bool CanBeCompleted
        {
            get
            {
                if (DependsOnTask == null) return true;
                return DependsOnTask.IsCompleted;
            }
        }
    }
}