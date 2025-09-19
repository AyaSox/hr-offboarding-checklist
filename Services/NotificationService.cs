using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using Microsoft.AspNetCore.Identity;

namespace OffboardingChecklist.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, NotificationType type, string recipientUserId, 
            NotificationPriority priority = NotificationPriority.Normal, string? actionUrl = null, string? actionText = null,
            int? relatedProcessId = null, int? relatedTaskId = null);
        
        Task CreateNotificationForRoleAsync(string title, string message, NotificationType type, string roleName,
            NotificationPriority priority = NotificationPriority.Normal, string? actionUrl = null, string? actionText = null,
            int? relatedProcessId = null, int? relatedTaskId = null);
        
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 50);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId, string userId);
        Task CleanupOldNotificationsAsync(int daysToKeep = 30);

        // Specific notification types
        Task NotifyProcessStartedAsync(OffboardingProcess process);
        Task NotifyProcessClosedAsync(OffboardingProcess process);
        Task NotifyTaskOverdueAsync(ChecklistItem task);
        Task NotifyTaskCompletedAsync(ChecklistItem task);
        Task NotifyTaskAssignedAsync(OffboardingProcess process, ChecklistItem task);
        
        // New approval workflow notifications
        Task NotifyProcessPendingApprovalAsync(OffboardingProcess process);
        Task NotifyProcessApprovedAsync(OffboardingProcess process);
        Task NotifyProcessRejectedAsync(OffboardingProcess process);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;

        public NotificationService(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<NotificationService> logger, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task CreateNotificationAsync(string title, string message, NotificationType type, string recipientUserId,
            NotificationPriority priority = NotificationPriority.Normal, string? actionUrl = null, string? actionText = null,
            int? relatedProcessId = null, int? relatedTaskId = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(recipientUserId);
                if (user == null) return;

                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    RecipientUserId = recipientUserId,
                    RecipientEmail = user.Email,
                    ActionUrl = actionUrl,
                    ActionText = actionText,
                    RelatedProcessId = relatedProcessId,
                    RelatedTaskId = relatedTaskId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Also send email to recipient
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(user.Email, title, $"<p>{message}</p>{(string.IsNullOrEmpty(actionUrl) ? string.Empty : $"<p><a href='{actionUrl}'>Go to item</a></p>")}");
                }

                _logger.LogInformation("Notification created for user {UserId}: {Title}", recipientUserId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for user {UserId}: {Title}", recipientUserId, title);
            }
        }

        public async Task CreateNotificationForRoleAsync(string title, string message, NotificationType type, string roleName,
            NotificationPriority priority = NotificationPriority.Normal, string? actionUrl = null, string? actionText = null,
            int? relatedProcessId = null, int? relatedTaskId = null)
        {
            try
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                
                // Also include users with HR/HC in their email for backward compatibility
                var hrUsers = await _userManager.Users
                    .Where(u => u.Email != null && (u.Email.ToLower().Contains("hr") || u.Email.ToLower().Contains("hc")))
                    .ToListAsync();
                
                var allUsers = usersInRole.Union(hrUsers).Distinct().ToList();

                foreach (var user in allUsers)
                {
                    await CreateNotificationAsync(title, message, type, user.Id, priority, actionUrl, actionText, relatedProcessId, relatedTaskId);
                }

                _logger.LogInformation("Notifications created for {UserCount} users in role {RoleName}: {Title}", allUsers.Count, roleName, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notifications for role {RoleName}: {Title}", roleName, title);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 50)
        {
            // Do not Include navigation properties because Notifications table has no FKs configured
            var query = _context.Notifications
                .Where(n => n.RecipientUserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedOn)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadOn = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CleanupOldNotificationsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedOn < cutoffDate && n.IsRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old notifications", oldNotifications.Count);
        }

        // Specific notification implementations
        public async Task NotifyProcessStartedAsync(OffboardingProcess process)
        {
            var title = $"New Offboarding Process Started";
            var message = $"A new offboarding process has been initiated for {process.EmployeeName} ({process.JobTitle}). Last working day: {process.LastWorkingDay:MMM dd, yyyy}";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";

            await CreateNotificationForRoleAsync(
                title, 
                message, 
                NotificationType.ProcessStarted, 
                ApplicationRoles.HR,
                NotificationPriority.Normal,
                actionUrl,
                "View Process",
                process.Id
            );
        }

        public async Task NotifyProcessClosedAsync(OffboardingProcess process)
        {
            var title = $"Offboarding Process Completed";
            var message = $"The offboarding process for {process.EmployeeName} has been successfully completed by {process.ClosedBy}.";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";

            await CreateNotificationForRoleAsync(
                title,
                message,
                NotificationType.ProcessClosed,
                ApplicationRoles.HR,
                NotificationPriority.Normal,
                actionUrl,
                "View Process",
                process.Id
            );

            // Email initiator
            if (!string.IsNullOrEmpty(process.InitiatedBy))
            {
                await _emailService.SendProcessCompletedEmailAsync(process.InitiatedBy, process.EmployeeName, process.ClosedBy ?? "System");
            }
        }

        public async Task NotifyTaskOverdueAsync(ChecklistItem task)
        {
            var daysPastDue = task.DueDate.HasValue ? (DateTime.Now.Date - task.DueDate.Value.Date).Days : 0;
            var title = $"Task Overdue - {task.OffboardingProcess.EmployeeName}";
            var message = $"Task '{task.TaskName}' is {daysPastDue} day(s) overdue in {task.Department} department.";
            var actionUrl = $"/OffboardingProcesses/Details/{task.OffboardingProcessId}";

            await CreateNotificationForRoleAsync(
                title,
                message,
                NotificationType.TaskOverdue,
                ApplicationRoles.HR,
                NotificationPriority.High,
                actionUrl,
                "View Task",
                task.OffboardingProcessId,
                task.Id
            );

            // Also notify Admin role
            await CreateNotificationForRoleAsync(
                title,
                message,
                NotificationType.TaskOverdue,
                ApplicationRoles.Admin,
                NotificationPriority.High,
                actionUrl,
                "View Task",
                task.OffboardingProcessId,
                task.Id
            );

            // Email HR and Admin users
            var hrUsers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.HR);
            var adminUsers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Admin);
            var allUsers = hrUsers.Union(adminUsers).Distinct();

            foreach (var user in allUsers)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOverdueTaskReminderAsync(user.Email, task.OffboardingProcess.EmployeeName, task.TaskName, daysPastDue);
                }
            }
        }

        public async Task NotifyTaskCompletedAsync(ChecklistItem task)
        {
            var title = $"Task Completed - {task.OffboardingProcess.EmployeeName}";
            var message = $"'{task.TaskName}' has been completed by {task.CompletedBy} in {task.Department} department.";
            var actionUrl = $"/OffboardingProcesses/Details/{task.OffboardingProcessId}";

            await CreateNotificationForRoleAsync(
                title,
                message,
                NotificationType.TaskCompleted,
                ApplicationRoles.HR,
                NotificationPriority.Low,
                actionUrl,
                "View Process",
                task.OffboardingProcessId,
                task.Id
            );

            // Email initiator
            if (!string.IsNullOrEmpty(task.OffboardingProcess.InitiatedBy))
            {
                await _emailService.SendTaskCompletedEmailAsync(task.OffboardingProcess.InitiatedBy, task.OffboardingProcess.EmployeeName, task.TaskName, task.CompletedBy ?? "");
            }
        }

        public async Task NotifyTaskAssignedAsync(OffboardingProcess process, ChecklistItem task)
        {
            // In-app: notify HR
            var title = $"Task Assigned - {process.EmployeeName}";
            var message = $"'{task.TaskName}' assigned to {task.Department}. Due: {process.LastWorkingDay:MMM dd, yyyy}.";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";
            await CreateNotificationForRoleAsync(title, message, NotificationType.ProcessStarted, ApplicationRoles.HR, NotificationPriority.Normal, actionUrl, "View", process.Id, task.Id);

            // Email the department distribution list directly
            var departmentEmail = GetDepartmentEmail(task.Department);
            if (!string.IsNullOrWhiteSpace(departmentEmail))
            {
                await _emailService.SendTaskAssignmentEmailAsync(departmentEmail, process.EmployeeName, task.TaskName, task.Department, "");
            }
        }

        private string GetDepartmentEmail(string department)
        {
            return department.ToLower() switch
            {
                "it" => "it@company.co.za",
                "hr" or "human capital" => "hr@company.co.za",
                "finance" => "finance@company.co.za",
                "payroll" => "payroll@company.co.za",
                _ => "hr@company.co.za"
            };
        }

        // New approval workflow notifications
        public async Task NotifyProcessPendingApprovalAsync(OffboardingProcess process)
        {
            var title = $"Process Approval Required";
            var message = $"Offboarding process for {process.EmployeeName} ({process.JobTitle}) is pending approval. Initiated by {process.InitiatedBy}.";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";

            // In-app for HR and Admin
            await CreateNotificationForRoleAsync(title, message, NotificationType.SystemAlert, ApplicationRoles.HR, NotificationPriority.High, actionUrl, "Review & Approve", process.Id);
            await CreateNotificationForRoleAsync(title, message, NotificationType.SystemAlert, ApplicationRoles.Admin, NotificationPriority.High, actionUrl, "Review & Approve", process.Id);

            // Email HR and Admin
            var recipients = new List<IdentityUser>();
            recipients.AddRange(await _userManager.GetUsersInRoleAsync(ApplicationRoles.HR));
            recipients.AddRange(await _userManager.GetUsersInRoleAsync(ApplicationRoles.Admin));
            foreach (var user in recipients.Distinct())
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(user.Email, title, $"<p>{message}</p><p><a href='{actionUrl}'>Review & Approve</a></p>");
                }
            }
        }

        public async Task NotifyProcessApprovedAsync(OffboardingProcess process)
        {
            var title = $"Process Approved";
            var message = $"Offboarding process for {process.EmployeeName} has been approved by {process.ApprovedBy} and is now active.";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";

            // Notify initiator (in-app + email)
            var initiator = await _userManager.FindByEmailAsync(process.InitiatedBy);
            if (initiator != null)
            {
                await CreateNotificationAsync(title, message, NotificationType.ProcessStarted, initiator.Id, NotificationPriority.Normal, actionUrl, "View Process", process.Id);
                await _emailService.SendEmailAsync(initiator.Email!, title, $"<p>{message}</p><p><a href='{actionUrl}'>View Process</a></p>");
            }

            // Also notify HR team in-app (email optional)
            await CreateNotificationForRoleAsync(title, message, NotificationType.ProcessStarted, ApplicationRoles.HR, NotificationPriority.Normal, actionUrl, "View Process", process.Id);
        }

        public async Task NotifyProcessRejectedAsync(OffboardingProcess process)
        {
            var title = $"Process Rejected";
            var message = $"Offboarding process for {process.EmployeeName} has been rejected by {process.RejectedBy}. Reason: {process.RejectionReason}";
            var actionUrl = $"/OffboardingProcesses/Details/{process.Id}";

            // Notify initiator (in-app + email)
            var initiator = await _userManager.FindByEmailAsync(process.InitiatedBy);
            if (initiator != null)
            {
                await CreateNotificationAsync(title, message, NotificationType.SystemAlert, initiator.Id, NotificationPriority.High, actionUrl, "View Details", process.Id);
                await _emailService.SendEmailAsync(initiator.Email!, title, $"<p>{message}</p><p><a href='{actionUrl}'>View Details</a></p>");
            }
        }
    }
}