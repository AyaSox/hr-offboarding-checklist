using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using System.Security.Claims;

namespace OffboardingChecklist.Controllers
{
    [Authorize(Roles = "HR,Admin")]
    public class TaskTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskTemplatesController> _logger;

        public TaskTemplatesController(ApplicationDbContext context, ILogger<TaskTemplatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: TaskTemplates
        public async Task<IActionResult> Index()
        {
            var templates = await _context.TaskTemplates
                .Include(t => t.DependsOnTemplate)
                .OrderBy(t => t.Department)
                .ThenBy(t => t.TaskName)
                .ToListAsync();

            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => d.Name)
                .OrderBy(n => n)
                .ToListAsync();

            return View(templates);
        }

        // GET: TaskTemplates/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await GetDepartmentSelectList();
            ViewBag.Templates = await GetTemplateSelectList();
            return View();
        }

        // POST: TaskTemplates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TaskName,Department,Description,DaysFromLastWorkingDay,IsRequired,DependsOnTemplateId")] TaskTemplate template)
        {
            if (ModelState.IsValid)
            {
                template.CreatedBy = User.Identity?.Name ?? "Unknown";
                template.CreatedOn = DateTime.Now;
                
                _context.Add(template);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Task template '{template.TaskName}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = await GetDepartmentSelectList();
            ViewBag.Templates = await GetTemplateSelectList();
            return View(template);
        }

        // GET: TaskTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var template = await _context.TaskTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            ViewBag.Departments = await GetDepartmentSelectList();
            ViewBag.Templates = await GetTemplateSelectList(id);
            return View(template);
        }

        // POST: TaskTemplates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TaskName,Department,Description,DaysFromLastWorkingDay,IsRequired,IsActive,DependsOnTemplateId")] TaskTemplate template)
        {
            if (id != template.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTemplate = await _context.TaskTemplates.FindAsync(id);
                    if (existingTemplate == null)
                    {
                        return NotFound();
                    }

                    existingTemplate.TaskName = template.TaskName;
                    existingTemplate.Department = template.Department;
                    existingTemplate.Description = template.Description;
                    existingTemplate.DaysFromLastWorkingDay = template.DaysFromLastWorkingDay;
                    existingTemplate.IsRequired = template.IsRequired;
                    existingTemplate.IsActive = template.IsActive;
                    existingTemplate.DependsOnTemplateId = template.DependsOnTemplateId;

                    _context.Update(existingTemplate);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Task template '{template.TaskName}' has been updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskTemplateExists(template.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = await GetDepartmentSelectList();
            ViewBag.Templates = await GetTemplateSelectList(id);
            return View(template);
        }

        // POST: TaskTemplates/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.TaskTemplates.FindAsync(id);
            if (template != null)
            {
                // Check if template has dependencies
                var hasDependents = await _context.TaskTemplates
                    .AnyAsync(t => t.DependsOnTemplateId == id);

                if (hasDependents)
                {
                    TempData["Error"] = $"Cannot delete template '{template.TaskName}' because other templates depend on it.";
                    return RedirectToAction(nameof(Index));
                }

                _context.TaskTemplates.Remove(template);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Task template '{template.TaskName}' has been deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: TaskTemplates/ImportFromCsv
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromCsv(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Error"] = "Please select a CSV file to import.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var templates = new List<TaskTemplate>();
                var createdBy = User.Identity?.Name ?? "Unknown";

                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                {
                    // Skip header row
                    await reader.ReadLineAsync();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length >= 4)
                        {
                            var template = new TaskTemplate
                            {
                                TaskName = values[0].Trim('"'),
                                Department = values[1].Trim('"'),
                                Description = values.Length > 2 ? values[2].Trim('"') : null,
                                DaysFromLastWorkingDay = values.Length > 3 && int.TryParse(values[3].Trim('"'), out int days) ? days : 0,
                                IsRequired = values.Length > 4 ? bool.Parse(values[4].Trim('"')) : true,
                                CreatedBy = createdBy,
                                CreatedOn = DateTime.Now
                            };
                            templates.Add(template);
                        }
                    }
                }

                _context.TaskTemplates.AddRange(templates);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully imported {templates.Count} task templates.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing task templates from CSV");
                TempData["Error"] = "Error importing CSV file. Please check the format and try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: TaskTemplates/ExportToCsv
        public async Task<IActionResult> ExportToCsv()
        {
            var templates = await _context.TaskTemplates
                .OrderBy(t => t.Department)
                .ThenBy(t => t.TaskName)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("TaskName,Department,Description,DaysFromLastWorkingDay,IsRequired,IsActive");

            foreach (var template in templates)
            {
                csv.AppendLine($"\"{template.TaskName}\",\"{template.Department}\",\"{template.Description}\",{template.DaysFromLastWorkingDay},{template.IsRequired},{template.IsActive}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"TaskTemplates_{DateTime.Now:yyyyMMdd}.csv");
        }

        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetDepartmentSelectList()
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = d.Name,
                    Text = d.Name
                })
                .ToListAsync();

            departments.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "Select Department" });
            return departments;
        }

        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetTemplateSelectList(int? excludeId = null)
        {
            var templates = await _context.TaskTemplates
                .Where(t => t.IsActive && (!excludeId.HasValue || t.Id != excludeId.Value))
                .OrderBy(t => t.Department)
                .ThenBy(t => t.TaskName)
                .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.Department} - {t.TaskName}"
                })
                .ToListAsync();

            templates.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "No Dependency" });
            return templates;
        }

        private bool TaskTemplateExists(int id)
        {
            return _context.TaskTemplates.Any(e => e.Id == id);
        }
    }
}