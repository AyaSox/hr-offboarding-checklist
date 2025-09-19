using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Data;
using OffboardingChecklist.Models;

namespace OffboardingChecklist.Services
{
    public interface ITaskGenerationService
    {
        Task<List<ChecklistItem>> CreateChecklistItemsForProcessAsync(OffboardingProcess process);
    }

    /// <summary>
    /// Generates checklist items from active task templates for a given process,
    /// including transferring template dependencies to the created tasks.
    /// </summary>
    public class TaskGenerationService : ITaskGenerationService
    {
        private readonly ApplicationDbContext _context;

        public TaskGenerationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChecklistItem>> CreateChecklistItemsForProcessAsync(OffboardingProcess process)
        {
            // Load active templates with dependency info
            var templates = await _context.TaskTemplates
                .Include(t => t.DependsOnTemplate)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Department)
                .ThenBy(t => t.TaskName)
                .ToListAsync();

            var createdItems = new List<ChecklistItem>();
            var templateToItem = new Dictionary<int, ChecklistItem>();

            if (!templates.Any())
            {
                return createdItems; // Nothing to create
            }

            // First pass: create items
            foreach (var template in templates)
            {
                var dueDate = process.LastWorkingDay.AddDays(template.DaysFromLastWorkingDay);
                var item = new ChecklistItem
                {
                    TaskName = template.TaskName,
                    Department = template.Department,
                    DueDate = dueDate,
                    OffboardingProcessId = process.Id
                };
                createdItems.Add(item);
                templateToItem[template.Id] = item;
            }

            _context.ChecklistItems.AddRange(createdItems);
            await _context.SaveChangesAsync(); // Ensure IDs are available

            // Second pass: wire up dependencies
            foreach (var template in templates.Where(t => t.DependsOnTemplateId.HasValue))
            {
                var dependentItem = templateToItem[template.Id];
                var dependsOnItem = templateToItem[template.DependsOnTemplateId!.Value];
                dependentItem.DependsOnTaskId = dependsOnItem.Id;
            }

            await _context.SaveChangesAsync();
            return createdItems;
        }
    }
}
