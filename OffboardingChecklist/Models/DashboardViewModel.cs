namespace OffboardingChecklist.Models
{
    public class DashboardViewModel
    {
        public int TotalProcesses { get; set; }
        public int CompletedProcesses { get; set; }
        public int PendingProcesses { get; set; }
        public double AverageProgress { get; set; }
        public List<OffboardingProcess> RecentProcesses { get; set; } = new List<OffboardingProcess>();
        public Dictionary<string, int> TasksByDepartment { get; set; } = new Dictionary<string, int>();
    }
}