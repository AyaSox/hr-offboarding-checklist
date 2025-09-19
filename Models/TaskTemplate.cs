using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public class TaskTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // 0 = on last working day, -1 = day before, positive = days after
        public int DaysFromLastWorkingDay { get; set; } = 0;

        public bool IsRequired { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int? DependsOnTemplateId { get; set; }
        public TaskTemplate? DependsOnTemplate { get; set; }

        public ICollection<TaskTemplate> DependentTemplates { get; set; } = new List<TaskTemplate>();

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
