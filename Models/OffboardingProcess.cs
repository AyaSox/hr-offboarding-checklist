using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public enum ProcessStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Active = 3,
        Closed = 4,
        Rejected = 5
    }

    public class OffboardingProcess
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee name is required")]
        [StringLength(100, ErrorMessage = "Employee name cannot exceed 100 characters")]
        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job title is required")]
        [StringLength(50, ErrorMessage = "Job title cannot exceed 50 characters")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employment start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Employment Start Date")]
        public DateTime EmploymentStartDate { get; set; }

        [Required(ErrorMessage = "Last working day is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Last Working Day")]
        [DateGreaterThan("EmploymentStartDate", ErrorMessage = "Last working day must be after employment start date")]
        public DateTime LastWorkingDay { get; set; }

        [Display(Name = "Process Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Initiated By")]
        public string InitiatedBy { get; set; } = string.Empty;

        // Enhanced status system
        public ProcessStatus Status { get; set; } = ProcessStatus.PendingApproval;

        [Display(Name = "Approval Status")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approved On")]
        public DateTime? ApprovedOn { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Rejected By")]
        public string? RejectedBy { get; set; }

        [Display(Name = "Rejected On")]
        public DateTime? RejectedOn { get; set; }

        [StringLength(100)]
        public string? ClosedBy { get; set; }
        public DateTime? ClosedOn { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[0];

        // Navigation properties
        public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
        public ICollection<OffboardingDocument> Documents { get; set; } = new List<OffboardingDocument>();

        // Computed properties
        public double YearsOfService
        {
            get
            {
                var endDate = IsClosed && ClosedOn.HasValue ? ClosedOn.Value : DateTime.UtcNow;
                return (endDate - EmploymentStartDate).TotalDays / 365.25;
            }
        }

        public int ProcessDurationDays => (DateTime.UtcNow - StartDate).Days;

        public bool IsActive => Status == ProcessStatus.Active && !IsClosed;

        public double ProgressPercent
        {
            get
            {
                if (!ChecklistItems.Any()) return 0;
                return (double)ChecklistItems.Count(c => c.IsCompleted) / ChecklistItems.Count() * 100;
            }
        }

        public int OverdueTasksCount => ChecklistItems.Count(c => c.IsOverdue);

        public string StatusText => Status switch
        {
            ProcessStatus.Draft => "Draft",
            ProcessStatus.PendingApproval => "Pending Approval",
            ProcessStatus.Approved => "Approved",
            ProcessStatus.Active => IsClosed ? "Completed" : (OverdueTasksCount > 0 ? "Overdue" : "On Track"),
            ProcessStatus.Closed => "Closed",
            ProcessStatus.Rejected => "Rejected",
            _ => "Unknown"
        };

        public string StatusColor => Status switch
        {
            ProcessStatus.Draft => "secondary",
            ProcessStatus.PendingApproval => "warning",
            ProcessStatus.Approved => "info",
            ProcessStatus.Active => OverdueTasksCount > 0 ? "danger" : "primary",
            ProcessStatus.Closed => "success",
            ProcessStatus.Rejected => "danger",
            _ => "secondary"
        };

        public string StatusIcon => Status switch
        {
            ProcessStatus.Draft => "fas fa-edit",
            ProcessStatus.PendingApproval => "fas fa-clock",
            ProcessStatus.Approved => "fas fa-check",
            ProcessStatus.Active => IsClosed ? "fas fa-check-circle" : "fas fa-play-circle",
            ProcessStatus.Closed => "fas fa-check-circle",
            ProcessStatus.Rejected => "fas fa-times-circle",
            _ => "fas fa-question-circle"
        };

        public bool CanBeEdited => Status == ProcessStatus.Draft || Status == ProcessStatus.PendingApproval;
        public bool CanBeApproved => Status == ProcessStatus.PendingApproval;
        public bool CanBeActivated => Status == ProcessStatus.Approved;
        public bool IsClosed { get; internal set; }
    }
}