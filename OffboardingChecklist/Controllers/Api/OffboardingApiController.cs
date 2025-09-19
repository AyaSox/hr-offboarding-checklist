using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using OffboardingChecklist.Services;

namespace OffboardingChecklist.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OffboardingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OffboardingApiController> _logger;

        public OffboardingApiController(ApplicationDbContext context, ILogger<OffboardingApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all offboarding processes with optional filtering
        /// </summary>
        [HttpGet("processes")]
        public async Task<ActionResult<IEnumerable<OffboardingProcessDto>>> GetProcesses(
            [FromQuery] bool? isClosed = null,
            [FromQuery] string? department = null,
            [FromQuery] int? daysOld = null)
        {
            var query = _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .AsQueryable();

            if (isClosed.HasValue)
                query = query.Where(p => p.IsClosed == isClosed.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(p => p.ChecklistItems.Any(c => c.Department == department));

            if (daysOld.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-daysOld.Value);
                query = query.Where(p => p.StartDate <= cutoffDate);
            }

            var processes = await query
                .Select(p => new OffboardingProcessDto
                {
                    Id = p.Id,
                    EmployeeName = p.EmployeeName,
                    JobTitle = p.JobTitle,
                    StartDate = p.StartDate,
                    InitiatedBy = p.InitiatedBy,
                    IsClosed = p.IsClosed,
                    ProgressPercent = p.ProgressPercent,
                    TaskCount = p.ChecklistItems.Count(),
                    CompletedTaskCount = p.ChecklistItems.Count(c => c.IsCompleted)
                })
                .ToListAsync();

            return Ok(processes);
        }

        /// <summary>
        /// Get specific offboarding process by ID
        /// </summary>
        [HttpGet("processes/{id}")]
        public async Task<ActionResult<OffboardingProcessDetailDto>> GetProcess(int id)
        {
            var process = await _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (process == null)
                return NotFound();

            var dto = new OffboardingProcessDetailDto
            {
                Id = process.Id,
                EmployeeName = process.EmployeeName,
                JobTitle = process.JobTitle,
                StartDate = process.StartDate,
                InitiatedBy = process.InitiatedBy,
                IsClosed = process.IsClosed,
                ProgressPercent = process.ProgressPercent,
                ChecklistItems = process.ChecklistItems.Select(c => new ChecklistItemDto
                {
                    Id = c.Id,
                    TaskName = c.TaskName,
                    Department = c.Department,
                    IsCompleted = c.IsCompleted,
                    CompletedBy = c.CompletedBy,
                    CompletedOn = c.CompletedOn,
                    Comments = c.Comments
                }).ToList()
            };

            return Ok(dto);
        }

        /// <summary>
        /// Create a new offboarding process
        /// </summary>
        [HttpPost("processes")]
        public async Task<ActionResult<OffboardingProcessDto>> CreateProcess([FromServices] ITaskGenerationService taskGenerationService, CreateOffboardingProcessDto dto)
        {
            var process = new OffboardingProcess
            {
                EmployeeName = dto.EmployeeName,
                JobTitle = dto.JobTitle,
                StartDate = DateTime.UtcNow,
                InitiatedBy = User.Identity?.Name ?? "API User",
                Status = ProcessStatus.Active,
                LastWorkingDay = DateTime.UtcNow.AddDays(14)
            };

            _context.OffboardingProcesses.Add(process);
            await _context.SaveChangesAsync();

            // Create tasks from templates via shared service
            var createdItems = await taskGenerationService.CreateChecklistItemsForProcessAsync(process);

            var result = new OffboardingProcessDto
            {
                Id = process.Id,
                EmployeeName = process.EmployeeName,
                JobTitle = process.JobTitle,
                StartDate = process.StartDate,
                InitiatedBy = process.InitiatedBy,
                IsClosed = process.IsClosed,
                ProgressPercent = process.ProgressPercent,
                TaskCount = createdItems.Count,
                CompletedTaskCount = 0
            };

            return CreatedAtAction(nameof(GetProcess), new { id = process.Id }, result);
        }

        /// <summary>
        /// Update task completion status
        /// </summary>
        [HttpPut("tasks/{taskId}/complete")]
        public async Task<IActionResult> CompleteTask(int taskId, [FromBody] CompleteTaskDto dto)
        {
            var task = await _context.ChecklistItems.FindAsync(taskId);
            if (task == null)
                return NotFound();

            task.IsCompleted = true;
            task.CompletedBy = User.Identity?.Name ?? "API User";
            task.CompletedOn = DateTime.UtcNow;
            task.Comments = dto.Comments;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get department statistics
        /// </summary>
        [HttpGet("analytics/departments")]
        public async Task<ActionResult<IEnumerable<DepartmentStatsDto>>> GetDepartmentStats()
        {
            var stats = await _context.ChecklistItems
                .GroupBy(c => c.Department)
                .Select(g => new DepartmentStatsDto
                {
                    Department = g.Key,
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(c => c.IsCompleted),
                    PendingTasks = g.Count(c => !c.IsCompleted),
                    CompletionRate = g.Count() > 0 ? (double)g.Count(c => c.IsCompleted) / g.Count() * 100 : 0
                })
                .ToListAsync();

            return Ok(stats);
        }

        /// <summary>
        /// Get overall system statistics
        /// </summary>
        [HttpGet("analytics/overview")]
        public async Task<ActionResult<SystemOverviewDto>> GetSystemOverview()
        {
            var totalProcesses = await _context.OffboardingProcesses.CountAsync();
            var activeProcesses = await _context.OffboardingProcesses.CountAsync(p => !p.IsClosed);
            var completedProcesses = await _context.OffboardingProcesses.CountAsync(p => p.IsClosed);
            
            var averageProgressQuery = _context.OffboardingProcesses
                .Where(p => !p.IsClosed)
                .Select(p => p.ChecklistItems.Count() > 0
                    ? (double)p.ChecklistItems.Count(c => c.IsCompleted) / p.ChecklistItems.Count() * 100
                    : 0);

            var averageProgress = await (averageProgressQuery.Any()
                ? averageProgressQuery.AverageAsync()
                : Task.FromResult(0.0));

            var overview = new SystemOverviewDto
            {
                TotalProcesses = totalProcesses,
                ActiveProcesses = activeProcesses,
                CompletedProcesses = completedProcesses,
                AverageProgress = averageProgress
            };

            return Ok(overview);
        }
    }

    // DTOs for API responses
    public class OffboardingProcessDto
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public double ProgressPercent { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
    }

    public class OffboardingProcessDetailDto : OffboardingProcessDto
    {
        public List<ChecklistItemDto> ChecklistItems { get; set; } = new();
    }

    public class ChecklistItemDto
    {
        public int Id { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string? CompletedBy { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string? Comments { get; set; }
    }

    public class CreateOffboardingProcessDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
    }

    public class CompleteTaskDto
    {
        public string? Comments { get; set; }
    }

    public class DepartmentStatsDto
    {
        public string Department { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public double CompletionRate { get; set; }
    }

    public class SystemOverviewDto
    {
        public int TotalProcesses { get; set; }
        public int ActiveProcesses { get; set; }
        public int CompletedProcesses { get; set; }
        public double AverageProgress { get; set; }
    }
}