using Microsoft.AspNetCore.Identity;

namespace OffboardingChecklist.Models
{
    public static class ApplicationRoles
    {
        public const string Admin = "Admin";
        public const string HR = "HR";
        public const string User = "User";
        public const string IT = "IT";
        public const string Finance = "Finance";
        public const string Payroll = "Payroll";
        public const string Manager = "Manager";
    }

    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}