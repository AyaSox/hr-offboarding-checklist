using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public class TaskComment
    {
        public int Id { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public int ChecklistItemId { get; set; }
        public ChecklistItem ChecklistItem { get; set; } = null!;
    }
}