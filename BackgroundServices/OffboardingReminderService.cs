using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Services;

namespace OffboardingChecklist.BackgroundServices
{
    public class OffboardingReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OffboardingReminderService> _logger;

        public OffboardingReminderService(IServiceProvider serviceProvider, ILogger<OffboardingReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Offboarding Reminder Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing reminders");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Retry in 30 minutes on error
                }
            }
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                // Send overdue task reminders
                await SendOverdueTaskRemindersAsync(context, emailService, notificationService);

                // Send completion notifications
                await SendCompletionNotificationsAsync(context, emailService, notificationService);

                // Clean up old notifications
                await CleanupOldNotificationsAsync(notificationService);

                // Clean up old closed processes (optional)
                await CleanupOldProcessesAsync(context);

                _logger.LogInformation("Reminder processing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessRemindersAsync");
                throw;
            }
        }

        private async Task SendOverdueTaskRemindersAsync(ApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
        {
            var overdueThreshold = DateTime.UtcNow.Date; // Tasks due today or earlier

            var overdueTasks = await context.ChecklistItems
                .Include(c => c.OffboardingProcess)
                .Where(c => !c.IsCompleted && 
                           c.DueDate.HasValue && 
                           c.DueDate.Value.Date <= overdueThreshold &&
                           !c.OffboardingProcess.IsClosed)
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                try
                {
                    var daysPastDue = (DateTime.UtcNow.Date - task.DueDate!.Value.Date).Days;

                    // Send in-app notification (always)
                    await notificationService.NotifyTaskOverdueAsync(task);

                    // Send email reminders to department and HR/Admin
                    var departmentEmail = await GetDepartmentEmailAsync(context, task.Department);
                    if (!string.IsNullOrEmpty(departmentEmail))
                    {
                        await emailService.SendOverdueTaskReminderAsync(
                            departmentEmail, 
                            task.OffboardingProcess.EmployeeName, 
                            task.TaskName,
                            daysPastDue);

                        _logger.LogInformation("Sent overdue email reminder for task '{TaskName}' to department {Email}", task.TaskName, departmentEmail);
                    }

                    // Also send to HR and Admin directly
                    await emailService.SendOverdueTaskReminderAsync(
                        "hr@company.co.za", 
                        task.OffboardingProcess.EmployeeName, 
                        task.TaskName,
                        daysPastDue);

                    await emailService.SendOverdueTaskReminderAsync(
                        "admin@company.co.za", 
                        task.OffboardingProcess.EmployeeName, 
                        task.TaskName,
                        daysPastDue);

                    _logger.LogInformation("Sent overdue notification for task '{TaskName}' ({DaysPastDue} days overdue)", task.TaskName, daysPastDue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for task {TaskName}", task.TaskName);
                    // Continue processing other tasks
                }
            }
        }

        private async Task SendCompletionNotificationsAsync(ApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-1); // Check processes completed in the last day

            var recentlyCompletedProcesses = await context.OffboardingProcesses
                .Where(p => p.IsClosed && p.ClosedOn.HasValue && p.ClosedOn.Value >= cutoffDate)
                .ToListAsync();

            foreach (var process in recentlyCompletedProcesses)
            {
                try
                {
                    if (!string.IsNullOrEmpty(process.InitiatedBy))
                    {
                        await emailService.SendProcessCompletedEmailAsync(
                            process.InitiatedBy,
                            process.EmployeeName,
                            process.ClosedBy ?? "System");
                    }

                    // In-app notify HR as well
                    await notificationService.NotifyProcessClosedAsync(process);

                    _logger.LogInformation("Sent completion notifications for {EmployeeName}", process.EmployeeName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send completion notification for {EmployeeName}", process.EmployeeName);
                    // Continue processing other processes
                }
            }
        }

        private async Task CleanupOldNotificationsAsync(INotificationService notificationService)
        {
            try
            {
                await notificationService.CleanupOldNotificationsAsync(30); // Keep notifications for 30 days
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old notifications");
            }
        }

        private async Task CleanupOldProcessesAsync(ApplicationDbContext context)
        {
            try
            {
                // Archive processes older than 2 years (optional)
                var archiveThreshold = DateTime.UtcNow.AddYears(-2);
                var oldProcessesCount = await context.OffboardingProcesses
                    .Where(p => p.IsClosed && p.ClosedOn.HasValue && p.ClosedOn.Value < archiveThreshold)
                    .CountAsync();

                if (oldProcessesCount > 0)
                {
                    _logger.LogInformation("Found {Count} old processes that could be archived", oldProcessesCount);
                    // Implement archiving logic here if needed
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old processes");
            }
        }

        private async Task<string> GetDepartmentEmailAsync(ApplicationDbContext context, string departmentName)
        {
            try
            {
                var department = await context.Departments
                    .FirstOrDefaultAsync(d => d.Name.ToLower() == departmentName.ToLower() && d.IsActive);
                
                if (department != null)
                {
                    return department.EmailAddress;
                }

                // Fallback to hardcoded mapping
                return departmentName.ToLower() switch
                {
                    "it" => "it@company.co.za",
                    "hr" or "human capital" => "hr@company.co.za",
                    "finance" => "finance@company.co.za",
                    "payroll" => "payroll@company.co.za",
                    _ => "hr@company.co.za"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get department email for {Department}", departmentName);
                return "hr@company.co.za"; // Fallback
            }
        }
    }
}