using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Models;
using Microsoft.AspNetCore.Identity;

namespace OffboardingChecklist.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Database migrations are applied at startup via Program.cs (context.Database.Migrate())
            // Avoid EnsureCreated here to prevent bypassing migrations.

            // Ensure all roles exist
            if (!await roleManager.RoleExistsAsync(ApplicationRoles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Admin));
            }

            if (!await roleManager.RoleExistsAsync(ApplicationRoles.HR))
            {
                await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.HR));
            }

            if (!await roleManager.RoleExistsAsync(ApplicationRoles.User))
            {
                await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.User));
            }

            // Create admin user
            var adminEmail = "admin@company.co.za";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
            }

            // Assign Admin role
            if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
            {
                await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
            }

            // Create HR user
            var hrEmail = "hr@company.co.za";
            var hrUser = await userManager.FindByEmailAsync(hrEmail);
            if (hrUser == null)
            {
                hrUser = new IdentityUser
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(hrUser, "HR123!");
            }

            // Assign HR role
            if (!await userManager.IsInRoleAsync(hrUser, ApplicationRoles.HR))
            {
                await userManager.AddToRoleAsync(hrUser, ApplicationRoles.HR);
            }

            // Create a regular user for testing
            var userEmail = "user@company.co.za";
            var regularUser = await userManager.FindByEmailAsync(userEmail);
            if (regularUser == null)
            {
                regularUser = new IdentityUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(regularUser, "User123!");
            }

            // Assign User role
            if (!await userManager.IsInRoleAsync(regularUser, ApplicationRoles.User))
            {
                await userManager.AddToRoleAsync(regularUser, ApplicationRoles.User);
            }

            // Seed processes if none exist
            if (!context.OffboardingProcesses.Any())
            {
                var process1 = new OffboardingProcess
                {
                    EmployeeName = "John Doe",
                    JobTitle = "Software Engineer",
                    EmploymentStartDate = new DateTime(2021, 3, 15), // Started 3+ years ago
                    LastWorkingDay = DateTime.Now.AddDays(7), // Leaving next week
                    StartDate = DateTime.Now.AddDays(-3),
                    InitiatedBy = "hr@company.co.za",
                    Status = ProcessStatus.Active,
                    ApprovedBy = "admin@company.co.za",
                    ApprovedOn = DateTime.Now.AddDays(-2)
                };

                var items1 = new List<ChecklistItem>
                {
                    new ChecklistItem { TaskName = "Return laptop and hardware", Department = "IT", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Disable access cards and accounts", Department = "IT", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Clear staff advances", Department = "Payroll", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Process final pay calculation", Department = "Payroll", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Credit card reconciliation", Department = "Finance", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Update termination on SAP & ESS", Department = "Human Capital", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Conduct exit interview", Department = "Human Capital", OffboardingProcess = process1, DueDate = process1.LastWorkingDay },
                    new ChecklistItem { TaskName = "Collect company property", Department = "Human Capital", OffboardingProcess = process1, DueDate = process1.LastWorkingDay }
                };

                foreach (var item in items1)
                {
                    process1.ChecklistItems.Add(item);
                }

                // Pending approval process
                var process2 = new OffboardingProcess
                {
                    EmployeeName = "Sarah Smith",
                    JobTitle = "Marketing Manager",
                    EmploymentStartDate = new DateTime(2019, 8, 20), // Started 5+ years ago
                    LastWorkingDay = DateTime.Now.AddDays(14), // Leaving in 2 weeks
                    StartDate = DateTime.Now.AddDays(-1),
                    InitiatedBy = "hr@company.co.za",
                    Status = ProcessStatus.PendingApproval
                };

                // Active process with some completed tasks
                var process3 = new OffboardingProcess
                {
                    EmployeeName = "Michael Johnson",
                    JobTitle = "Finance Analyst",
                    EmploymentStartDate = new DateTime(2020, 1, 10),
                    LastWorkingDay = DateTime.Now.AddDays(-5), // Already left
                    StartDate = DateTime.Now.AddDays(-15),
                    InitiatedBy = "hr@company.co.za",
                    Status = ProcessStatus.Active,
                    ApprovedBy = "admin@company.co.za",
                    ApprovedOn = DateTime.Now.AddDays(-12)
                };

                var items3 = new List<ChecklistItem>
                {
                    new ChecklistItem { TaskName = "Return laptop and hardware", Department = "IT", OffboardingProcess = process3, IsCompleted = true, CompletedBy = "it@company.co.za", CompletedOn = DateTime.Now.AddDays(-10), DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Disable access cards and accounts", Department = "IT", OffboardingProcess = process3, IsCompleted = true, CompletedBy = "it@company.co.za", CompletedOn = DateTime.Now.AddDays(-8), DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Clear staff advances", Department = "Payroll", OffboardingProcess = process3, IsCompleted = true, CompletedBy = "payroll@company.co.za", CompletedOn = DateTime.Now.AddDays(-6), DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Process final pay calculation", Department = "Payroll", OffboardingProcess = process3, DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Credit card reconciliation", Department = "Finance", OffboardingProcess = process3, IsCompleted = true, CompletedBy = "finance@company.co.za", CompletedOn = DateTime.Now.AddDays(-4), DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Update termination on SAP & ESS", Department = "Human Capital", OffboardingProcess = process3, DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Conduct exit interview", Department = "Human Capital", OffboardingProcess = process3, IsCompleted = true, CompletedBy = "hr@company.co.za", CompletedOn = DateTime.Now.AddDays(-7), DueDate = process3.LastWorkingDay },
                    new ChecklistItem { TaskName = "Collect company property", Department = "Human Capital", OffboardingProcess = process3, DueDate = process3.LastWorkingDay }
                };

                foreach (var item in items3)
                {
                    process3.ChecklistItems.Add(item);
                }

                // Closed process
                var process4 = new OffboardingProcess
                {
                    EmployeeName = "Lisa Williams",
                    JobTitle = "Project Manager",
                    EmploymentStartDate = new DateTime(2018, 5, 15),
                    LastWorkingDay = DateTime.Now.AddDays(-30),
                    StartDate = DateTime.Now.AddDays(-35),
                    InitiatedBy = "hr@company.co.za",
                    Status = ProcessStatus.Closed,
                    ApprovedBy = "admin@company.co.za",
                    ApprovedOn = DateTime.Now.AddDays(-32),
                    IsClosed = true,
                    ClosedBy = "hr@company.co.za",
                    ClosedOn = DateTime.Now.AddDays(-25)
                };

                var items4 = new List<ChecklistItem>
                {
                    new ChecklistItem { TaskName = "Return laptop and hardware", Department = "IT", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "it@company.co.za", CompletedOn = DateTime.Now.AddDays(-32), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Disable access cards and accounts", Department = "IT", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "it@company.co.za", CompletedOn = DateTime.Now.AddDays(-30), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Clear staff advances", Department = "Payroll", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "payroll@company.co.za", CompletedOn = DateTime.Now.AddDays(-28), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Process final pay calculation", Department = "Payroll", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "payroll@company.co.za", CompletedOn = DateTime.Now.AddDays(-27), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Credit card reconciliation", Department = "Finance", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "finance@company.co.za", CompletedOn = DateTime.Now.AddDays(-26), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Update termination on SAP & ESS", Department = "Human Capital", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "hr@company.co.za", CompletedOn = DateTime.Now.AddDays(-26), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Conduct exit interview", Department = "Human Capital", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "hr@company.co.za", CompletedOn = DateTime.Now.AddDays(-25), DueDate = process4.LastWorkingDay },
                    new ChecklistItem { TaskName = "Collect company property", Department = "Human Capital", OffboardingProcess = process4, IsCompleted = true, CompletedBy = "hr@company.co.za", CompletedOn = DateTime.Now.AddDays(-25), DueDate = process4.LastWorkingDay }
                };

                foreach (var item in items4)
                {
                    process4.ChecklistItems.Add(item);
                }

                context.OffboardingProcesses.Add(process1);
                context.OffboardingProcesses.Add(process2);
                context.OffboardingProcesses.Add(process3);
                context.OffboardingProcesses.Add(process4);
                await context.SaveChangesAsync();
            }

            // Seed departments if none exist
            if (!context.Departments.Any())
            {
                var departments = new[]
                {
                    new Department { Name = "IT", EmailAddress = "it@company.co.za", Description = "Information Technology Department", CreatedBy = "System" },
                    new Department { Name = "Human Capital", EmailAddress = "hr@company.co.za", Description = "Human Resources Department", CreatedBy = "System" },
                    new Department { Name = "Finance", EmailAddress = "finance@company.co.za", Description = "Finance Department", CreatedBy = "System" },
                    new Department { Name = "Payroll", EmailAddress = "payroll@company.co.za", Description = "Payroll Department", CreatedBy = "System" }
                };

                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();
            }

            // Seed task templates if none exist
            if (!context.TaskTemplates.Any())
            {
                var templates = new[]
                {
                    new TaskTemplate { TaskName = "Return laptop and hardware", Department = "IT", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Disable access cards and accounts", Department = "IT", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Update team distribution lists", Department = "IT", DaysFromLastWorkingDay = 1, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Clear staff advances", Department = "Payroll", DaysFromLastWorkingDay = -5, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Process final pay calculation", Department = "Payroll", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Credit card reconciliation", Department = "Finance", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Update termination on SAP & ESS", Department = "Human Capital", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Conduct exit interview", Department = "Human Capital", DaysFromLastWorkingDay = -1, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Collect company property", Department = "Human Capital", DaysFromLastWorkingDay = 0, IsRequired = true, CreatedBy = "System" },
                    new TaskTemplate { TaskName = "Knowledge transfer documentation", Department = "Human Capital", DaysFromLastWorkingDay = -3, IsRequired = true, CreatedBy = "System" }
                };

                context.TaskTemplates.AddRange(templates);
                await context.SaveChangesAsync();
            }
        }

        // Keep backward compatibility
        public static void Seed(ApplicationDbContext context)
        {
            // Deprecated: Do not use. Left intentionally empty to avoid bypassing migrations.
        }
    }
}