using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;

namespace OffboardingChecklist.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var processes = await _context.OffboardingProcesses
                .Include(p => p.ChecklistItems)
                .ToListAsync();

            var dashboard = new DashboardViewModel
            {
                TotalProcesses = processes.Count,
                CompletedProcesses = processes.Count(p => p.IsClosed),
                PendingProcesses = processes.Count(p => !p.IsClosed),
                AverageProgress = processes.Any() ? processes.Average(p => p.ProgressPercent) : 0,
                RecentProcesses = processes.OrderByDescending(p => p.StartDate).Take(5).ToList(),
                TasksByDepartment = await _context.ChecklistItems
                    .GroupBy(c => c.Department)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Department, x => x.Count)
            };

            return View(dashboard);
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
