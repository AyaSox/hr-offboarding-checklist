namespace OffboardingChecklist.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipientEmail, string subject, string body);
        Task SendTaskAssignmentEmailAsync(string recipientEmail, string employeeName, string taskName, string department, string priority = "");
        Task SendTaskCompletedEmailAsync(string recipientEmail, string employeeName, string taskName, string completedBy);
        Task SendProcessCompletedEmailAsync(string recipientEmail, string employeeName, string completedBy);
        Task SendOverdueTaskReminderAsync(string recipientEmail, string employeeName, string taskName, int daysPastDue);
        Task SendBulkReminderEmailAsync(List<string> recipientEmails, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            // Demo implementation - in production, use SendGrid, SMTP, etc.
            _logger.LogInformation("Email sent to {RecipientEmail} with subject: {Subject}", recipientEmail, subject);
            await Task.Delay(100); // Simulate async email sending
        }

        public async Task SendTaskAssignmentEmailAsync(string recipientEmail, string employeeName, string taskName, string department, string priority = "")
        {
            var subject = $"New Task - {employeeName}";
            var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .card {{ border: 1px solid #e1e1e1; border-radius: 8px; overflow: hidden; margin: 20px 0; }}
                    .header {{ background: linear-gradient(135deg, #007bff 0%, #0056b3 100%); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 30px; background: #f8f9fa; }}
                    .task-details {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #0d6efd; }}
                    .btn {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: 600; margin: 10px 0; }}
                    .footer {{ background: #e9ecef; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    td {{ padding: 8px 0; vertical-align: top; }}
                    .label {{ font-weight: 600; width: 120px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='card'>
                        <div class='header'>
                            <h1 style='margin: 0; font-size: 24px;'>New Task Assignment</h1>
                            <p style='margin: 5px 0 0 0; opacity: 0.9;'>Employee Offboarding System</p>
                        </div>
                        
                        <div class='content'>
                            <div class='task-details'>
                                <h3 style='margin: 0 0 15px 0; color: #495057;'>Task Assignment Details</h3>
                                
                                <table>
                                    <tr><td class='label'>Employee:</td><td><strong>{employeeName}</strong></td></tr>
                                    <tr><td class='label'>Task:</td><td>{taskName}</td></tr>
                                    <tr><td class='label'>Department:</td><td>{department}</td></tr>
                                    <tr><td class='label'>Assigned:</td><td>{DateTime.Now:MMM dd, yyyy HH:mm}</td></tr>
                                </table>
                            </div>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='margin-bottom: 20px; font-size: 16px;'>Please complete this task to ensure smooth employee offboarding.</p>
                                <a href='#' class='btn'>View in System</a>
                            </div>
                        </div>
                        
                        <div class='footer'>
                            <p style='margin: 0;'>
                                This is an automated message from the Employee Offboarding System.<br>
                                Please do not reply to this email.
                            </p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        public async Task SendTaskCompletedEmailAsync(string recipientEmail, string employeeName, string taskName, string completedBy)
        {
            var subject = $"Task Completed - {employeeName}";
            var body = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e1e1e1; border-radius: 8px;'>
                    <div style='text-align: center; background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 20px; border-radius: 6px; margin-bottom: 20px;'>
                        <h1 style='margin: 0;'>Task Completed</h1>
                        <p style='margin: 5px 0 0 0; opacity: 0.9;'>Offboarding Progress Update</p>
                    </div>
                    
                    <div style='background: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #495057;'>Completion Details</h3>
                        <p><strong>Employee:</strong> {employeeName}</p>
                        <p><strong>Task:</strong> {taskName}</p>
                        <p><strong>Completed By:</strong> {completedBy}</p>
                        <p><strong>Completed On:</strong> {DateTime.Now:MMM dd, yyyy HH:mm}</p>
                    </div>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <p>Great job! The offboarding process is progressing smoothly.</p>
                        <a href='#' style='display: inline-block; background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: 600;'>View Process Status</a>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        public async Task SendProcessCompletedEmailAsync(string recipientEmail, string employeeName, string completedBy)
        {
            var subject = $"Offboarding Process Completed - {employeeName}";
            var body = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e1e1e1; border-radius: 8px;'>
                    <div style='text-align: center; background: linear-gradient(135deg, #6f42c1 0%, #e83e8c 100%); color: white; padding: 30px; border-radius: 6px; margin-bottom: 20px;'>
                        <h1 style='margin: 0; font-size: 28px;'>Process Completed</h1>
                        <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>Offboarding Successfully Finished</p>
                    </div>
                    
                    <div style='background: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #495057;'>Process Summary</h3>
                        <p><strong>Employee:</strong> {employeeName}</p>
                        <p><strong>Completed By:</strong> {completedBy}</p>
                        <p><strong>Completion Date:</strong> {DateTime.Now:MMM dd, yyyy HH:mm}</p>
                        <p><strong>Status:</strong> All Tasks Completed</p>
                    </div>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <p style='font-size: 16px;'>The employee offboarding process has been successfully completed. All required tasks have been finished and the process is now closed.</p>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        public async Task SendOverdueTaskReminderAsync(string recipientEmail, string employeeName, string taskName, int daysPastDue)
        {
            var subject = $"OVERDUE: Task Reminder - {employeeName}";
            var body = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e1e1e1; border-radius: 8px;'>
                    <div style='text-align: center; background: linear-gradient(135deg, #dc3545 0%, #fd7e14 100%); color: white; padding: 20px; border-radius: 6px; margin-bottom: 20px;'>
                        <h1 style='margin: 0;'>Overdue Task</h1>
                        <p style='margin: 5px 0 0 0; opacity: 0.9;'>Immediate Attention Required</p>
                    </div>
                    
                    <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #856404;'>Overdue Task Details</h3>
                        <p><strong>Employee:</strong> {employeeName}</p>
                        <p><strong>Task:</strong> {taskName}</p>
                        <p><strong>Days Overdue:</strong> <span style='color: #dc3545; font-weight: bold;'>{daysPastDue} days</span></p>
                        <p><strong>Reminder Sent:</strong> {DateTime.Now:MMM dd, yyyy HH:mm}</p>
                    </div>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <p style='color: #dc3545; font-weight: bold;'>This task is overdue and requires immediate attention to avoid delays in the offboarding process.</p>
                        <a href='#' style='display: inline-block; background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: 600;'>Complete Task Now</a>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        public async Task SendBulkReminderEmailAsync(List<string> recipientEmails, string subject, string body)
        {
            foreach (var email in recipientEmails)
            {
                await SendEmailAsync(email, subject, body);
                await Task.Delay(100); // Small delay to prevent overwhelming
            }
        }
    }
}