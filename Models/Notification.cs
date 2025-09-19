using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public enum NotificationType
    {
        ProcessStarted,
        ProcessClosed, 
        TaskOverdue,
        TaskCompleted,
        SystemAlert,
        Reminder
    }

    public enum NotificationPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        [Required]
        public string RecipientUserId { get; set; } = string.Empty;

        public string? RecipientEmail { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? ReadOn { get; set; }

        public string? ActionUrl { get; set; }

        public string? ActionText { get; set; }

        // Store IDs but no navigation properties to avoid FK issues
        public int? RelatedProcessId { get; set; }
        public int? RelatedTaskId { get; set; }

        // Helper properties for UI
        public string IconClass => Type switch
        {
            NotificationType.ProcessStarted => "fas fa-play-circle text-success",
            NotificationType.ProcessClosed => "fas fa-check-circle text-primary",
            NotificationType.TaskOverdue => "fas fa-exclamation-triangle text-danger",
            NotificationType.TaskCompleted => "fas fa-check text-success",
            NotificationType.SystemAlert => "fas fa-bell text-warning",
            NotificationType.Reminder => "fas fa-clock text-info",
            _ => "fas fa-info-circle text-secondary"
        };

        public string PriorityClass => Priority switch
        {
            NotificationPriority.Critical => "border-danger",
            NotificationPriority.High => "border-warning",
            NotificationPriority.Normal => "border-info",
            NotificationPriority.Low => "border-secondary",
            _ => "border-secondary"
        };

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedOn;
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays}d ago";
                else if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours}h ago";
                else if (timeSpan.TotalMinutes >= 1)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                else
                    return "Just now";
            }
        }

        // Navigation properties for manual loading when needed
        public OffboardingProcess? RelatedProcess { get; set; }
        public ChecklistItem? RelatedTask { get; set; }
    }
}