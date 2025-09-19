using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using OffboardingChecklist.Services;
using System.Security.Claims;

namespace OffboardingChecklist.Controllers
{
    [Authorize]
    public class OffboardingProcessesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OffboardingProcessesController> _logger;
        private readonly ITaskGenerationService _taskGenerationService;

        public OffboardingProcessesController(ApplicationDbContext context, IEmailService emailService, INotificationService notificationService, ILogger<OffboardingProcessesController> logger, ITaskGenerationService taskGenerationService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
            _taskGenerationService = taskGenerationService;
        }

        // Consistent identifier helper (email if available, else Name, else Unknown)
        private string GetCurrentUserIdentifier()
            => User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "Unknown";

        // Helper method to check if user is HR/Admin
        private bool IsHROrAdmin()
        {
            return User.IsInRole(ApplicationRoles.HR) || User.IsInRole(ApplicationRoles.Admin);
        }

        // GET: OffboardingProcesses
        public async Task<IActionResult> Index(string searchString, string statusFilter, string departmentFilter, string sortOrder, DateTime? startFrom, DateTime? startTo, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["DepartmentFilter"] = departmentFilter;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["StartFrom"] = startFrom?.ToString("yyyy-MM-dd");
            ViewData["StartTo"] = startTo?.ToString("yyyy-MM-dd");
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["ProgressSortParm"] = sortOrder == "Progress" ? "progress_desc" : "Progress";

            var processes = _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .AsQueryable();

            var isHRorAdmin = IsHROrAdmin();
            var userIdentifier = GetCurrentUserIdentifier();

            // Access control: HR/Admin see all; others only their own initiated processes
            if (!isHRorAdmin)
            {
                processes = processes.Where(p => p.InitiatedBy == userIdentifier);
            }

            // Search filter
            if (!String.IsNullOrEmpty(searchString))
            {
                processes = processes.Where(p => p.EmployeeName.Contains(searchString) ||
                                                p.JobTitle.Contains(searchString) ||
                                                p.InitiatedBy.Contains(searchString));
            }

            // Date range filter (by StartDate)
            if (startFrom.HasValue)
            {
                var fromDate = startFrom.Value.Date;
                processes = processes.Where(p => p.StartDate.Date >= fromDate);
            }
            if (startTo.HasValue)
            {
                var toDate = startTo.Value.Date;
                processes = processes.Where(p => p.StartDate.Date <= toDate);
            }

            // Status filter
            if (!String.IsNullOrEmpty(statusFilter))
            {
                switch (statusFilter.ToLower())
                {
                    case "pending":
                        processes = processes.Where(p => p.Status == ProcessStatus.PendingApproval);
                        break;
                    case "approved":
                        processes = processes.Where(p => p.Status == ProcessStatus.Approved);
                        break;
                    case "active":
                        processes = processes.Where(p => p.Status == ProcessStatus.Active && !p.IsClosed);
                        break;
                    case "closed":
                        processes = processes.Where(p => p.IsClosed || p.Status == ProcessStatus.Closed);
                        break;
                    case "rejected":
                        processes = processes.Where(p => p.Status == ProcessStatus.Rejected);
                        break;
                    case "overdue":
                        processes = processes.Where(p => p.Status == ProcessStatus.Active && !p.IsClosed && p.ChecklistItems.Any(c => c.IsOverdue));
                        break;
                }
            }

            // Department filter
            if (!String.IsNullOrEmpty(departmentFilter))
            {
                processes = processes.Where(p => p.ChecklistItems.Any(c => c.Department == departmentFilter));
            }

            // Sorting
            processes = sortOrder switch
            {
                "name_desc" => processes.OrderByDescending(p => p.EmployeeName),
                "Date" => processes.OrderBy(p => p.StartDate),
                "date_desc" => processes.OrderByDescending(p => p.StartDate),
                "Progress" => processes.OrderBy(p => p.ChecklistItems.Count(c => c.IsCompleted)),
                "progress_desc" => processes.OrderByDescending(p => p.ChecklistItems.Count(c => c.IsCompleted)),
                _ => processes.OrderBy(p => p.EmployeeName),
            };

            var totalCount = await processes.CountAsync();
            var pageNumber = page < 1 ? 1 : page;
            var size = pageSize <= 0 ? 10 : pageSize;
            var items = await processes.Skip((pageNumber - 1) * size).Take(size).ToListAsync();

            // Get department list for filter dropdown
            ViewBag.Departments = await _context.ChecklistItems
                .Select(c => c.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.IsHROrAdmin = isHRorAdmin;
            ViewBag.UserEmail = userIdentifier;

            // Pagination metadata
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = pageNumber;
            ViewBag.PageSize = size;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)size);

            return View(items);
        }

        // GET: OffboardingProcesses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offboardingProcess = await _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .ThenInclude(c => c.TaskComments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offboardingProcess == null)
            {
                return NotFound();
            }

            var userIdentifier = GetCurrentUserIdentifier();
            var isHRorAdmin = IsHROrAdmin();

            // Access: HR/Admin can view all; others only their own initiated processes
            bool canView = isHRorAdmin || offboardingProcess.InitiatedBy == userIdentifier;

            if (!canView)
            {
                TempData["Error"] = "You don't have permission to view this process.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsHR = isHRorAdmin;
            ViewBag.IsInitiator = (offboardingProcess.InitiatedBy == userIdentifier);

            return View(offboardingProcess);
        }

        // GET: OffboardingProcesses/Create - Any authenticated user
        public IActionResult Create()
        {
            // No HR/Admin check here - now any authenticated user can create a process request
            var model = new OffboardingProcess
            {
                StartDate = DateTime.Now,
                EmploymentStartDate = DateTime.Now.AddYears(-2), // Default 2 years ago
                LastWorkingDay = DateTime.Now.AddDays(14), // Default 2 weeks notice
                Status = ProcessStatus.PendingApproval
            };
            return View(model);
        }

        // POST: OffboardingProcesses/Create - Any authenticated user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeName,JobTitle,EmploymentStartDate,LastWorkingDay")] OffboardingProcess offboardingProcess)
        {
            // No HR/Admin check here - any authenticated user can submit a process request

            // Populate system-controlled fields BEFORE validation
            var initiatorEmail = GetCurrentUserIdentifier();
            offboardingProcess.InitiatedBy = initiatorEmail;
            offboardingProcess.StartDate = DateTime.Now;
            offboardingProcess.Status = ProcessStatus.PendingApproval; // Always starts as pending approval

            // Since InitiatedBy is not bound, clear potential model state errors for it
            ModelState.Remove(nameof(OffboardingProcess.InitiatedBy));
            ModelState.Remove(nameof(OffboardingProcess.StartDate));
            ModelState.Remove(nameof(OffboardingProcess.Status));

            if (ModelState.IsValid)
            {
                _context.Add(offboardingProcess);
                await _context.SaveChangesAsync();

                // Send approval notification to HR/Admin
                await _notificationService.NotifyProcessPendingApprovalAsync(offboardingProcess);

                TempData["Success"] = $"Offboarding request for {offboardingProcess.EmployeeName} has been submitted for approval. HR/Admin team will review shortly.";
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, validation failed - show the form with errors
            return View(offboardingProcess);
        }

        // POST: Approve Process - Only HR/Admin
        [HttpPost]
        [Authorize(Roles = ApplicationRoles.HR + "," + ApplicationRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProcess(int id)
        {
            if (!IsHROrAdmin())
            {
                TempData["Error"] = "Only HR and Admin users can approve processes.";
                return RedirectToAction("Details", new { id });
            }

            var process = await _context.OffboardingProcesses
                .FirstOrDefaultAsync(p => p.Id == id);

            if (process != null && process.Status == ProcessStatus.PendingApproval)
            {
                process.Status = ProcessStatus.Active;
                process.ApprovedBy = GetCurrentUserIdentifier();
                process.ApprovedOn = DateTime.UtcNow;

                // Generate tasks from templates (with dependencies) using centralized service
                var createdItems = await _taskGenerationService.CreateChecklistItemsForProcessAsync(process);

                // Send approval notification
                await _notificationService.NotifyProcessApprovedAsync(process);

                // Send assignment emails for tasks
                foreach (var task in createdItems)
                {
                    await _notificationService.NotifyTaskAssignedAsync(process, task);
                }

                TempData["Success"] = $"Offboarding process for {process.EmployeeName} has been approved and activated. Task assignments sent to departments.";
            }
            else
            {
                TempData["Error"] = "Process not found or not in pending approval status.";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: Reject Process - Only HR/Admin
        [HttpPost]
        [Authorize(Roles = ApplicationRoles.HR + "," + ApplicationRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProcess(int id, string rejectionReason)
        {
            if (!IsHROrAdmin())
            {
                TempData["Error"] = "Only HR and Admin users can reject processes.";
                return RedirectToAction("Details", new { id });
            }

            var process = await _context.OffboardingProcesses
                .FirstOrDefaultAsync(p => p.Id == id);

            if (process != null && process.Status == ProcessStatus.PendingApproval)
            {
                process.Status = ProcessStatus.Rejected;
                process.RejectedBy = GetCurrentUserIdentifier();
                process.RejectedOn = DateTime.UtcNow;
                process.RejectionReason = rejectionReason ?? "No reason provided";

                await _context.SaveChangesAsync();

                // Send rejection notification
                await _notificationService.NotifyProcessRejectedAsync(process);

                TempData["Warning"] = $"Offboarding process for {process.EmployeeName} has been rejected. Initiator has been notified.";
            }
            else
            {
                TempData["Error"] = "Process not found or not in pending approval status.";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: CompleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteItem(int itemId, string? comments)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var processId = 0;

            try
            {
                var item = await _context.ChecklistItems
                    .Include(i => i.OffboardingProcess)
                    .Include(i => i.TaskComments)
                    .Include(i => i.DependsOnTask)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item != null)
                {
                    processId = item.OffboardingProcessId;

                    // Check if process is active
                    if (item.OffboardingProcess.Status != ProcessStatus.Active)
                    {
                        TempData["Error"] = "Tasks can only be completed for active processes.";
                        return RedirectToAction("Details", new { id = processId });
                    }

                    // Check if dependencies are met
                    if (!item.CanBeCompleted)
                    {
                        TempData["Error"] = $"This task cannot be completed until '{item.DependsOnTask?.TaskName}' is completed first.";
                        return RedirectToAction("Details", new { id = processId });
                    }

                    var currentUser = GetCurrentUserIdentifier();
                    item.IsCompleted = true;
                    item.CompletedBy = currentUser;
                    item.CompletedOn = DateTime.UtcNow;
                    item.Comments = comments;

                    // Add comment to history if provided
                    if (!string.IsNullOrWhiteSpace(comments))
                    {
                        var taskComment = new TaskComment
                        {
                            Comment = comments,
                            CreatedBy = currentUser,
                            ChecklistItemId = itemId,
                            CreatedOn = DateTime.UtcNow
                        };
                        _context.TaskComments.Add(taskComment);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Send in-app notification
                    await _notificationService.NotifyTaskCompletedAsync(item);

                    // Send completion notification
                    await _emailService.SendTaskCompletedEmailAsync(
                        item.OffboardingProcess.InitiatedBy,
                        item.OffboardingProcess.EmployeeName,
                        item.TaskName,
                        item.CompletedBy);

                    TempData["Success"] = $"Task '{item.TaskName}' has been marked as completed!";
                }
                else
                {
                    TempData["Error"] = "Task not found.";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "This task was modified by another user. Please refresh and try again.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing task {ItemId}", itemId);
                TempData["Error"] = "An error occurred while completing the task. Please try again.";
            }

            return RedirectToAction("Details", new { id = processId });
        }

        // POST: UncompleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UncompleteItem(int itemId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var processId = 0;

            try
            {
                var item = await _context.ChecklistItems
                    .Include(i => i.OffboardingProcess)
                    .Include(i => i.DependentTasks)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item != null)
                {
                    processId = item.OffboardingProcessId;

                    // Check if process is active
                    if (item.OffboardingProcess.Status != ProcessStatus.Active)
                    {
                        TempData["Error"] = "Tasks can only be modified for active processes.";
                        return RedirectToAction("Details", new { id = processId });
                    }

                    // Security check - only the person who completed it or HR/Admin can uncomplete
                    var identifier = GetCurrentUserIdentifier();
                    var isHRorAdmin = IsHROrAdmin();

                    if (!isHRorAdmin && item.CompletedBy != identifier)
                    {
                        TempData["Error"] = "You don't have permission to modify this task completion. Only the person who completed the task or HR/Admin can undo it.";
                        return RedirectToAction("Details", new { id = processId });
                    }

                    // Check if there are dependent tasks that are completed
                    var completedDependentTasks = item.DependentTasks.Where(t => t.IsCompleted).ToList();
                    if (completedDependentTasks.Any())
                    {
                        var dependentTaskNames = string.Join(", ", completedDependentTasks.Select(t => t.TaskName));
                        TempData["Error"] = $"Cannot mark this task as incomplete because the following dependent tasks are already completed: {dependentTaskNames}";
                        return RedirectToAction("Details", new { id = processId });
                    }

                    var taskName = item.TaskName;
                    item.IsCompleted = false;
                    item.CompletedBy = null;
                    item.CompletedOn = null;
                    item.Comments = null;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Warning"] = $"Task '{taskName}' has been marked as incomplete.";
                }
                else
                {
                    TempData["Error"] = "Task not found.";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "This task was modified by another user. Please refresh and try again.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error uncompleting task {ItemId}", itemId);
                TempData["Error"] = "An error occurred while modifying the task. Please try again.";
            }

            return RedirectToAction("Details", new { id = processId });
        }

        // POST: Delete Process - Only HR/Admin with safety checks
        [HttpPost]
        [Authorize(Roles = ApplicationRoles.HR + "," + ApplicationRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProcess(int id, string confirmationText)
        {
            if (!IsHROrAdmin())
            {
                TempData["Error"] = "Only HR and Admin personnel can delete offboarding processes.";
                return RedirectToAction("Details", new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var process = await _context.OffboardingProcesses
                    .Include(p => p.ChecklistItems)
                    .ThenInclude(c => c.TaskComments)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (process == null)
                {
                    TempData["Error"] = "Process not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Safety checks - only allow deletion of certain processes
                if (process.Status == ProcessStatus.Active && process.ChecklistItems.Any(c => c.IsCompleted))
                {
                    TempData["Error"] = "Cannot delete an active process with completed tasks. Close the process instead.";
                    return RedirectToAction("Details", new { id });
                }

                if (process.Status == ProcessStatus.Closed)
                {
                    TempData["Error"] = "Cannot delete a closed process. Closed processes should be retained for record keeping.";
                    return RedirectToAction("Details", new { id });
                }

                // Require confirmation text
                if (string.IsNullOrWhiteSpace(confirmationText) || 
                    !confirmationText.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Please type 'DELETE' in the confirmation field to confirm deletion.";
                    return RedirectToAction("Details", new { id });
                }

                var employeeName = process.EmployeeName;
                var deletedBy = GetCurrentUserIdentifier();

                // Delete related data first (due to foreign key constraints)
                var taskComments = await _context.TaskComments
                    .Where(tc => tc.ChecklistItem.OffboardingProcessId == id)
                    .ToListAsync();
                _context.TaskComments.RemoveRange(taskComments);

                // Delete checklist items
                _context.ChecklistItems.RemoveRange(process.ChecklistItems);

                // Delete documents if any
                var documents = await _context.OffboardingDocuments
                    .Where(d => d.OffboardingProcessId == id)
                    .ToListAsync();
                _context.OffboardingDocuments.RemoveRange(documents);

                // Delete notifications related to this process
                var notifications = await _context.Notifications
                    .Where(n => n.RelatedProcessId == id)
                    .ToListAsync();
                _context.Notifications.RemoveRange(notifications);

                // Finally delete the process
                _context.OffboardingProcesses.Remove(process);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log the deletion for audit purposes
                _logger.LogWarning("Process deleted: Employee={EmployeeName}, ProcessId={ProcessId}, DeletedBy={DeletedBy}, Status={Status}, TaskCount={TaskCount}, InitiatedBy={InitiatedBy}", 
                    employeeName, id, deletedBy, process.Status, process.ChecklistItems.Count, process.InitiatedBy);

                TempData["Warning"] = $"Offboarding process for {employeeName} has been permanently deleted. This action cannot be undone.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting process {ProcessId}", id);
                TempData["Error"] = "An error occurred while deleting the process. Please try again.";
                return RedirectToAction("Details", new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CloseProcess - HR Only
        [HttpPost]
        [Authorize(Roles = ApplicationRoles.HR + "," + ApplicationRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseProcess(int id)
        {
            if (!IsHROrAdmin())
            {
                TempData["Error"] = "Only HR and Admin personnel can close offboarding processes.";
                return RedirectToAction("Details", new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var process = await _context.OffboardingProcesses
                    .Include(p => p.ChecklistItems)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (process != null)
                {
                    // Check if process is active
                    if (process.Status != ProcessStatus.Active)
                    {
                        TempData["Error"] = "Only active processes can be closed.";
                        return RedirectToAction("Details", new { id });
                    }

                    // Check if all tasks are completed
                    var incompleteTasks = await _context.ChecklistItems
                        .Where(c => c.OffboardingProcessId == id && !c.IsCompleted)
                        .CountAsync();

                    if (incompleteTasks > 0)
                    {
                        TempData["Warning"] = $"Cannot close process: {incompleteTasks} task(s) still pending completion.";
                        return RedirectToAction("Details", new { id });
                    }

                    process.IsClosed = true;
                    process.Status = ProcessStatus.Closed;
                    process.ClosedBy = GetCurrentUserIdentifier();
                    process.ClosedOn = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Send in-app notification
                    await _notificationService.NotifyProcessClosedAsync(process);

                    // Send process completion notification
                    await _emailService.SendProcessCompletedEmailAsync(
                        process.InitiatedBy,
                        process.EmployeeName,
                        process.ClosedBy);

                    TempData["Success"] = $"Offboarding process for {process.EmployeeName} has been closed successfully!";
                }
                else
                {
                    TempData["Error"] = "Process not found.";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "This process was modified by another user. Please refresh and try again.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error closing process {ProcessId}", id);
                TempData["Error"] = "An error occurred while closing the process. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: Bulk Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkComplete(int[] selectedTasks, string? bulkComments)
        {
            if (selectedTasks?.Length > 0)
            {
                try
                {
                    var currentUser = GetCurrentUserIdentifier();
                    var isHRorAdmin = IsHROrAdmin();

                    var tasksQuery = _context.ChecklistItems
                        .Include(c => c.OffboardingProcess)
                        .Where(c => selectedTasks.Contains(c.Id) && !c.IsCompleted && c.OffboardingProcess.Status == ProcessStatus.Active);

                    if (!isHRorAdmin)
                    {
                        tasksQuery = tasksQuery.Where(c => c.OffboardingProcess.InitiatedBy == currentUser);
                    }

                    var tasks = await tasksQuery.ToListAsync();

                    foreach (var task in tasks)
                    {
                        task.IsCompleted = true;
                        task.CompletedBy = currentUser;
                        task.CompletedOn = DateTime.UtcNow;
                        task.Comments = bulkComments;

                        if (!string.IsNullOrWhiteSpace(bulkComments))
                        {
                            var taskComment = new TaskComment
                            {
                                Comment = bulkComments,
                                CreatedBy = currentUser,
                                ChecklistItemId = task.Id,
                                CreatedOn = DateTime.UtcNow
                            };
                            _context.TaskComments.Add(taskComment);
                        }
                    }

                    await _context.SaveChangesAsync();

                    foreach (var task in tasks)
                    {
                        await _notificationService.NotifyTaskCompletedAsync(task);
                    }

                    TempData["Success"] = $"Successfully completed {tasks.Count} tasks!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in bulk complete operation");
                    TempData["Error"] = "An error occurred while completing tasks. Please try again.";
                }
            }
            else
            {
                TempData["Warning"] = "No tasks selected for bulk completion.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Get tasks for selected processes (for bulk actions)
        [HttpPost]
        public async Task<IActionResult> GetTasksForProcesses([FromBody] GetTasksRequest request)
        {
            try
            {
                if (request?.ProcessIds == null || !request.ProcessIds.Any())
                {
                    return Json(new { success = false, message = "No processes selected" });
                }

                var isHRorAdmin = IsHROrAdmin();
                var currentUser = GetCurrentUserIdentifier();

                var tasksQuery = _context.ChecklistItems
                    .Include(c => c.OffboardingProcess)
                    .Where(c => request.ProcessIds.Contains(c.OffboardingProcessId) &&
                                !c.IsCompleted &&
                                c.OffboardingProcess.Status == ProcessStatus.Active);

                if (!isHRorAdmin)
                {
                    tasksQuery = tasksQuery.Where(c => c.OffboardingProcess.InitiatedBy == currentUser);
                }

                var tasks = await tasksQuery
                    .Select(c => new
                    {
                        id = c.Id,
                        taskName = c.TaskName,
                        department = c.Department,
                        employeeName = c.OffboardingProcess.EmployeeName,
                        dueDate = c.DueDate,
                        isOverdue = c.IsOverdue
                    })
                    .OrderBy(c => c.employeeName)
                    .ThenBy(c => c.department)
                    .ToListAsync();

                return Json(new { success = true, tasks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for processes");
                return Json(new { success = false, message = "Error loading tasks" });
            }
        }

        public class GetTasksRequest
        {
            public List<int> ProcessIds { get; set; } = new();
        }

        // GET: Export CSV for current filters
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string searchString, string statusFilter, string departmentFilter, string sortOrder, DateTime? startFrom, DateTime? startTo)
        {
            var processes = _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .AsQueryable();

            var isHRorAdmin = IsHROrAdmin();
            var userIdentifier = GetCurrentUserIdentifier();
            if (!isHRorAdmin)
            {
                processes = processes.Where(p => p.InitiatedBy == userIdentifier);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                processes = processes.Where(p => p.EmployeeName.Contains(searchString) || p.JobTitle.Contains(searchString) || p.InitiatedBy.Contains(searchString));
            }

            if (startFrom.HasValue)
            {
                var fromDate = startFrom.Value.Date;
                processes = processes.Where(p => p.StartDate.Date >= fromDate);
            }
            if (startTo.HasValue)
            {
                var toDate = startTo.Value.Date;
                processes = processes.Where(p => p.StartDate.Date <= toDate);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                switch (statusFilter.ToLower())
                {
                    case "pending": processes = processes.Where(p => p.Status == ProcessStatus.PendingApproval); break;
                    case "approved": processes = processes.Where(p => p.Status == ProcessStatus.Approved); break;
                    case "active": processes = processes.Where(p => p.Status == ProcessStatus.Active && !p.IsClosed); break;
                    case "closed": processes = processes.Where(p => p.IsClosed || p.Status == ProcessStatus.Closed); break;
                    case "rejected": processes = processes.Where(p => p.Status == ProcessStatus.Rejected); break;
                    case "overdue": processes = processes.Where(p => p.Status == ProcessStatus.Active && !p.IsClosed && p.ChecklistItems.Any(c => c.IsOverdue)); break;
                }
            }

            if (!string.IsNullOrEmpty(departmentFilter))
            {
                processes = processes.Where(p => p.ChecklistItems.Any(c => c.Department == departmentFilter));
            }

            processes = sortOrder switch
            {
                "name_desc" => processes.OrderByDescending(p => p.EmployeeName),
                "Date" => processes.OrderBy(p => p.StartDate),
                "date_desc" => processes.OrderByDescending(p => p.StartDate),
                "Progress" => processes.OrderBy(p => p.ChecklistItems.Count(c => c.IsCompleted)),
                "progress_desc" => processes.OrderByDescending(p => p.ChecklistItems.Count(c => c.IsCompleted)),
                _ => processes.OrderBy(p => p.EmployeeName),
            };

            var list = await processes.ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Employee Name,Job Title,Initiated By,Status,Start Date,Last Working Day,Tasks Completed,Total Tasks");
            foreach (var p in list)
            {
                var completed = p.ChecklistItems.Count(c => c.IsCompleted);
                var total = p.ChecklistItems.Count;
                var line = string.Join(',', new[]
                {
                    EscapeCsv(p.EmployeeName),
                    EscapeCsv(p.JobTitle),
                    EscapeCsv(p.InitiatedBy),
                    EscapeCsv(p.StatusText),
                    p.StartDate.ToString("yyyy-MM-dd"),
                    p.LastWorkingDay.ToString("yyyy-MM-dd"),
                    completed.ToString(),
                    total.ToString()
                });
                sb.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"offboarding_processes_{DateTime.Now:yyyyMMddHHmmss}.csv");

            static string EscapeCsv(string? input)
            {
                input ??= string.Empty;
                if (input.Contains('"') || input.Contains(',') || input.Contains('\n'))
                {
                    input = input.Replace("\"", "\"\"");
                    return $"\"{input}\"";
                }
                return input;
            }
        }
    }
}