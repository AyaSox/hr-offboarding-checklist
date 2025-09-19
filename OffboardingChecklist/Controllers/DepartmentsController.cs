using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;
using System.Security.Claims;

namespace OffboardingChecklist.Controllers
{
    [Authorize(Roles = "HR,Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(ApplicationDbContext context, ILogger<DepartmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(departments);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,EmailAddress,ManagerName,ManagerEmail,Description")] Department department)
        {
            if (ModelState.IsValid)
            {
                department.CreatedBy = User.Identity?.Name ?? "Unknown";
                department.CreatedOn = DateTime.Now;
                
                _context.Add(department);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Department '{department.Name}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,EmailAddress,ManagerName,ManagerEmail,IsActive,Description")] Department department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDept = await _context.Departments.FindAsync(id);
                    if (existingDept == null)
                    {
                        return NotFound();
                    }

                    existingDept.Name = department.Name;
                    existingDept.EmailAddress = department.EmailAddress;
                    existingDept.ManagerName = department.ManagerName;
                    existingDept.ManagerEmail = department.ManagerEmail;
                    existingDept.IsActive = department.IsActive;
                    existingDept.Description = department.Description;

                    _context.Update(existingDept);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Department '{department.Name}' has been updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
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
            return View(department);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                // Check if department is used in any checklist items
                var itemsUsingDept = await _context.ChecklistItems
                    .AnyAsync(c => c.Department == department.Name);

                if (itemsUsingDept)
                {
                    TempData["Error"] = $"Cannot delete department '{department.Name}' because it is used in existing checklist items. Consider deactivating it instead.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Department '{department.Name}' has been deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }

        // API endpoint for getting department email
        [HttpGet]
        public async Task<IActionResult> GetDepartmentEmail(string name)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower() && d.IsActive);

            if (department != null)
            {
                return Json(new { email = department.EmailAddress });
            }

            return Json(new { email = "" });
        }
    }
}