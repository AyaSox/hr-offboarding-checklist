using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ManagerName { get; set; }

        [StringLength(200)]
        [EmailAddress]
        public string? ManagerEmail { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }
}