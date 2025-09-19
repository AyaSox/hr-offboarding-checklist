namespace OffboardingChecklist.Models
{
    public class AnalyticsViewModel
    {
        // Basic metrics
        public int TotalProcesses { get; set; }
        public int CompletedProcesses { get; set; }
        public int ActiveProcesses { get; set; }
        public int PendingApprovalProcesses { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageCompletionDays { get; set; }

        // Collections
        public List<DepartmentPerformance> DepartmentPerformance { get; set; } = new();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
        public List<TopPerformer> TopPerformers { get; set; } = new();
        public List<TaskTypeCompletion> TaskCompletionByType { get; set; } = new();
        public List<RecentActivity> RecentActivity { get; set; } = new();

        // Computed properties
        public double CompletionRate => TotalProcesses > 0 ? (double)CompletedProcesses / TotalProcesses * 100 : 0;
        public double TaskCompletionRate => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
    }

    public class DepartmentPerformance
    {
        public string Department { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageCompletionDays { get; set; }
        public double CompletionRate => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
        public int PendingTasks => TotalTasks - CompletedTasks;
    }

    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ProcessesStarted { get; set; }
        public int ProcessesCompleted { get; set; }
        public double AverageCompletionDays { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class TopPerformer
    {
        public string Name { get; set; } = string.Empty;
        public int TasksCompleted { get; set; }
        public double AverageCompletionDays { get; set; }
        public string PerformanceRating => AverageCompletionDays <= 0 ? "Excellent" : 
                                          AverageCompletionDays <= 2 ? "Good" : "Needs Improvement";
    }

    public class TaskTypeCompletion
    {
        public string TaskType { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public double AverageCompletionDays { get; set; }
        public double CompletionRate => TotalAssigned > 0 ? (double)Completed / TotalAssigned * 100 : 0;
    }

    public class RecentActivity
    {
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string CompletedBy { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Date;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} days ago";
                return Date.ToString("MMM dd, yyyy");
            }
        }
    }
}