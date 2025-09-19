using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using OffboardingChecklist.Services;

namespace OffboardingChecklist.Controllers
{
    [Authorize(Roles = ApplicationRoles.HR + "," + ApplicationRoles.Admin)]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public AnalyticsController(ApplicationDbContext context, ILogger<AnalyticsController> logger, IMemoryCache cache, INotificationService notificationService, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var analytics = new AnalyticsViewModel();
            var warnings = new List<string>();
            var today = DateTime.Today;

            // Overview metrics
            try
            {
                analytics.TotalProcesses = await _context.OffboardingProcesses.CountAsync();
                analytics.CompletedProcesses = await _context.OffboardingProcesses.CountAsync(p => p.IsClosed);
                analytics.ActiveProcesses = await _context.OffboardingProcesses.CountAsync(p => p.Status == ProcessStatus.Active && !p.IsClosed);
                analytics.PendingApprovalProcesses = await _context.OffboardingProcesses.CountAsync(p => p.Status == ProcessStatus.PendingApproval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: overview metrics failed");
                warnings.Add("Overview metrics are temporarily unavailable.");
            }

            // Task metrics
            try
            {
                analytics.TotalTasks = await _context.ChecklistItems.CountAsync();
                analytics.CompletedTasks = await _context.ChecklistItems.CountAsync(t => t.IsCompleted);
                analytics.OverdueTasks = await _context.ChecklistItems
                    .CountAsync(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && !t.IsCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: task metrics failed");
                warnings.Add("Task metrics are temporarily unavailable.");
            }

            // Department performance
            try
            {
                analytics.DepartmentPerformance = await _context.ChecklistItems
                    .GroupBy(t => t.Department)
                    .Select(g => new DepartmentPerformance
                    {
                        Department = g.Key,
                        TotalTasks = g.Count(),
                        CompletedTasks = g.Count(t => t.IsCompleted),
                        OverdueTasks = g.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && !t.IsCompleted),
                        AverageCompletionDays = g.Where(t => t.IsCompleted && t.CompletedOn.HasValue && t.DueDate.HasValue)
                            .Select(t => (t.CompletedOn!.Value - t.DueDate!.Value).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average()
                    })
                    .Where(d => d.TotalTasks > 0)
                    .OrderByDescending(d => d.CompletionRate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: department performance failed");
                warnings.Add("Department performance is temporarily unavailable.");
            }

            // Monthly trends (last 6 months including current)
            try
            {
                var sixMonthsBack = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
                analytics.MonthlyTrends = await _context.OffboardingProcesses
                    .Where(p => p.StartDate >= sixMonthsBack)
                    .GroupBy(p => new { Year = p.StartDate.Year, Month = p.StartDate.Month })
                    .Select(g => new MonthlyTrend
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        ProcessesStarted = g.Count(),
                        ProcessesCompleted = g.Count(p => p.IsClosed && p.ClosedOn.HasValue && p.ClosedOn.Value.Year == g.Key.Year && p.ClosedOn.Value.Month == g.Key.Month),
                        AverageCompletionDays = g.Where(p => p.IsClosed && p.ClosedOn.HasValue)
                            .Select(p => (p.ClosedOn!.Value - p.StartDate).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average()
                    })
                    .OrderBy(t => t.Year).ThenBy(t => t.Month)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: monthly trends failed");
                warnings.Add("Monthly trends are temporarily unavailable.");
            }

            // Average completion days
            try
            {
                analytics.AverageCompletionDays = await _context.OffboardingProcesses
                    .Where(p => p.IsClosed && p.ClosedOn.HasValue)
                    .Select(p => (p.ClosedOn!.Value - p.StartDate).TotalDays)
                    .DefaultIfEmpty(0)
                    .AverageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: average completion calculation failed");
                warnings.Add("Average completion time is temporarily unavailable.");
            }

            // Top performers
            try
            {
                analytics.TopPerformers = await _context.ChecklistItems
                    .Where(t => t.IsCompleted && !string.IsNullOrEmpty(t.CompletedBy))
                    .GroupBy(t => t.CompletedBy)
                    .Select(g => new TopPerformer
                    {
                        Name = g.Key!,
                        TasksCompleted = g.Count(),
                        AverageCompletionDays = g.Where(t => t.CompletedOn.HasValue && t.DueDate.HasValue)
                            .Select(t => (t.CompletedOn!.Value - t.DueDate!.Value).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average()
                    })
                    .OrderByDescending(p => p.TasksCompleted)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: top performers failed");
                warnings.Add("Top performers are temporarily unavailable.");
            }

            // Task completion by type
            try
            {
                analytics.TaskCompletionByType = await _context.ChecklistItems
                    .GroupBy(t => t.TaskName)
                    .Select(g => new TaskTypeCompletion
                    {
                        TaskType = g.Key,
                        TotalAssigned = g.Count(),
                        Completed = g.Count(t => t.IsCompleted),
                        AverageCompletionDays = g.Where(t => t.IsCompleted && t.CompletedOn.HasValue && t.DueDate.HasValue)
                            .Select(t => (t.CompletedOn!.Value - t.DueDate!.Value).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average()
                    })
                    .Where(t => t.TotalAssigned >= 3)
                    .OrderByDescending(t => t.CompletionRate)
                    .Take(15)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: task completion by type failed");
                warnings.Add("Task completion breakdown is temporarily unavailable.");
            }

            // Recent activity
            try
            {
                analytics.RecentActivity = await _context.ChecklistItems
                    .Where(t => t.CompletedOn.HasValue && t.CompletedOn.Value >= DateTime.Now.AddDays(-30))
                    .Include(t => t.OffboardingProcess)
                    .OrderByDescending(t => t.CompletedOn)
                    .Select(t => new RecentActivity
                    {
                        Date = t.CompletedOn!.Value,
                        Action = $"Task '{t.TaskName}' completed for {t.OffboardingProcess.EmployeeName}",
                        CompletedBy = t.CompletedBy ?? "Unknown",
                        Department = t.Department
                    })
                    .Take(20)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics: recent activity failed");
                warnings.Add("Recent activity is temporarily unavailable.");
            }

            if (warnings.Any())
            {
                ViewBag.AnalyticsWarnings = warnings;
            }

            return View(analytics);
        }

        // Test endpoint to manually trigger overdue reminders
        [HttpPost]
        public async Task<IActionResult> TestOverdueReminders()
        {
            try
            {
                var overdueThreshold = DateTime.Now.Date;
                var overdueTasks = await _context.ChecklistItems
                    .Include(c => c.OffboardingProcess)
                    .Where(c => !c.IsCompleted && 
                               c.DueDate.HasValue && 
                               c.DueDate.Value.Date <= overdueThreshold &&
                               !c.OffboardingProcess.IsClosed)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count);

                foreach (var task in overdueTasks)
                {
                    var daysPastDue = (DateTime.Now.Date - task.DueDate!.Value.Date).Days;
                    _logger.LogInformation("Processing overdue task: {TaskName} for {Employee} ({DaysPastDue} days overdue)", 
                        task.TaskName, task.OffboardingProcess.EmployeeName, daysPastDue);

                    // Send notification
                    await _notificationService.NotifyTaskOverdueAsync(task);
                    
                    // Send email reminder to HR
                    await _emailService.SendOverdueTaskReminderAsync(
                        "hr@company.co.za", 
                        task.OffboardingProcess.EmployeeName, 
                        task.TaskName, 
                        daysPastDue);
                }

                TempData["Success"] = $"Processed {overdueTasks.Count} overdue tasks. Check notifications and console logs.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestOverdueReminders");
                TempData["Error"] = $"Error processing reminders: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType)
        {
            try
            {
                _logger.LogInformation("GetChartData called with chartType: {ChartType}", chartType);

                var cacheKey = $"chart:{chartType?.ToLower()}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                {
                    return Json(cached!);
                }

                object result = chartType?.ToLower() switch
                {
                    "departmentperformance" => await GetDepartmentPerformanceChartData(),
                    "monthlytrends" => await GetMonthlyTrendsChartData(),
                    "taskcompletion" => await GetTaskCompletionChartData(),
                    "processstatus" => await GetProcessStatusChartData(),
                    _ => new { error = $"Invalid chart type: {chartType}" }
                };

                if (result is not null && (result as IResult) == null)
                {
                    _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(90)
                    });
                }

                return chartType?.ToLower() switch
                {
                    "departmentperformance" or "monthlytrends" or "taskcompletion" or "processstatus" => Json(result),
                    _ => BadRequest($"Invalid chart type: {chartType}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for {ChartType}", chartType);
                return StatusCode(500, new { error = "Error loading chart data", message = ex.Message });
            }
        }

        // Test endpoint to verify routes are working
        [HttpGet]
        public IActionResult Test()
        {
            _logger.LogInformation("Analytics Test endpoint called");
            return Json(new { message = "Analytics controller is working", timestamp = DateTime.Now });
        }

        private async Task<object> GetDepartmentPerformanceChartData()
        {
            try
            {
                var today = DateTime.Today;
                var data = await _context.ChecklistItems
                    .GroupBy(t => t.Department)
                    .Select(g => new
                    {
                        department = g.Key,
                        totalTasks = g.Count(),
                        completedTasks = g.Count(t => t.IsCompleted),
                        overdueTasks = g.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && !t.IsCompleted)
                    })
                    .Where(d => d.totalTasks > 0)
                    .OrderBy(d => d.department)
                    .ToListAsync();

                if (!data.Any())
                {
                    return new
                    {
                        labels = new string[] { "No Data" },
                        datasets = new[]
                        {
                            new
                            {
                                label = "No Data Available",
                                data = new int[] { 0 },
                                backgroundColor = "#dee2e6"
                            }
                        }
                    };
                }

                return new
                {
                    labels = data.Select(d => d.department).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Completed Tasks",
                            data = data.Select(d => d.completedTasks).ToArray(),
                            backgroundColor = "#28a745"
                        },
                        new
                        {
                            label = "Overdue Tasks",
                            data = data.Select(d => d.overdueTasks).ToArray(),
                            backgroundColor = "#dc3545"
                        },
                        new
                        {
                            label = "Pending Tasks",
                            data = data.Select(d => d.totalTasks - d.completedTasks - d.overdueTasks).ToArray(),
                            backgroundColor = "#ffc107"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDepartmentPerformanceChartData");
                throw;
            }
        }

        private async Task<object> GetMonthlyTrendsChartData()
        {
            try
            {
                var sixMonthsBack = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
                var data = await _context.OffboardingProcesses
                    .Where(p => p.StartDate >= sixMonthsBack)
                    .GroupBy(p => new { Year = p.StartDate.Year, Month = p.StartDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Started = g.Count(),
                        Completed = g.Count(p => p.IsClosed && p.ClosedOn.HasValue && p.ClosedOn.Value.Year == g.Key.Year && p.ClosedOn.Value.Month == g.Key.Month)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                if (!data.Any())
                {
                    var label = DateTime.Today.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    return new
                    {
                        labels = new[] { label },
                        datasets = new[]
                        {
                            new { label = "Processes Started", data = new[] { 0 }, borderColor = "#007bff", backgroundColor = "rgba(0,123,255,0.1)", fill = true },
                            new { label = "Processes Completed", data = new[] { 0 }, borderColor = "#28a745", backgroundColor = "rgba(40,167,69,0.1)", fill = true }
                        }
                    };
                }

                var labels = data.Select(x => new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var started = data.Select(x => x.Started).ToArray();
                var completed = data.Select(x => x.Completed).ToArray();

                return new
                {
                    labels,
                    datasets = new object[]
                    {
                        new { label = "Processes Started", data = started, borderColor = "#007bff", backgroundColor = "rgba(0,123,255,0.1)", fill = true },
                        new { label = "Processes Completed", data = completed, borderColor = "#28a745", backgroundColor = "rgba(40,167,69,0.1)", fill = true }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMonthlyTrendsChartData");
                throw;
            }
        }

        private async Task<object> GetTaskCompletionChartData()
        {
            try
            {
                var data = await _context.ChecklistItems
                    .GroupBy(t => t.TaskName)
                    .Select(g => new
                    {
                        taskName = g.Key,
                        total = g.Count(),
                        completed = g.Count(t => t.IsCompleted)
                    })
                    .Where(t => t.total >= 3)
                    .OrderByDescending(t => (double)t.completed / t.total)
                    .Take(10)
                    .ToListAsync();

                if (!data.Any())
                {
                    data = await _context.ChecklistItems
                        .GroupBy(t => t.TaskName)
                        .Select(g => new
                        {
                            taskName = g.Key,
                            total = g.Count(),
                            completed = g.Count(t => t.IsCompleted)
                        })
                        .OrderByDescending(t => (double)t.completed / t.total)
                        .Take(5)
                        .ToListAsync();
                }

                if (!data.Any())
                {
                    return new
                    {
                        labels = new string[] { "No Tasks" },
                        datasets = new[]
                        {
                            new
                            {
                                label = "Completion Rate %",
                                data = new double[] { 0 },
                                backgroundColor = new string[] { "#dee2e6" }
                            }
                        }
                    };
                }

                return new
                {
                    labels = data.Select(d => d.taskName).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Completion Rate %",
                            data = data.Select(d => Math.Round((double)d.completed / d.total * 100, 1)).ToArray(),
                            backgroundColor = data.Select((d, index) => 
                            {
                                var rate = (double)d.completed / d.total;
                                return rate >= 0.9 ? "#28a745" : rate >= 0.7 ? "#ffc107" : "#dc3545";
                            }).ToArray()
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTaskCompletionChartData");
                throw;
            }
        }

        private async Task<object> GetProcessStatusChartData()
        {
            var statusCounts = await _context.OffboardingProcesses
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    status = g.Key.ToString(),
                    count = g.Count()
                })
                .ToListAsync();

            return new
            {
                labels = statusCounts.Select(s => s.status).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        data = statusCounts.Select(s => s.count).ToArray(),
                        backgroundColor = new[] { "#ffc107", "#17a2b8", "#007bff", "#dc3545", "#28a745" }
                    }
                }
            };
        }
    }
}