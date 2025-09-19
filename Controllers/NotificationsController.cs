using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OffboardingChecklist.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace OffboardingChecklist.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger, UserManager<IdentityUser> userManager)
        {
            _notificationService = notificationService;
            _logger = logger;
            _userManager = userManager;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string GetCurrentUserIdentifier() => User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "Unknown";

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index", "Home");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }

        // GET: API endpoint for notification count
        [HttpGet]
        [Route("Notifications/GetUnreadCount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Json(new { count = 0 });

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count for {User}", GetCurrentUserIdentifier());
                return Json(new { count = 0 });
            }
        }

        // GET: API endpoint for recent notifications
        [HttpGet]
        [Route("Notifications/GetLatest")]
        public async Task<IActionResult> GetLatest()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Json(new List<object>());

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, false, 10);
                var result = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = (int)n.Type,
                    priority = (int)n.Priority,
                    isRead = n.IsRead,
                    createdOn = n.CreatedOn,
                    timeAgo = n.TimeAgo,
                    actionUrl = n.ActionUrl ?? "/Notifications",
                    actionText = n.ActionText ?? "View"
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load recent notifications for user {UserId}", GetUserId());
                return Json(new List<object>());
            }
        }

        public class MarkRequest { public int id { get; set; } }

        // POST: Mark notification as read (accept JSON body)
        [HttpPost]
        [Route("Notifications/MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (!string.IsNullOrEmpty(userId) && request != null && request.id > 0)
                {
                    await _notificationService.MarkAsReadAsync(request.id, userId);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Invalid request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification as read for {User}", GetCurrentUserIdentifier());
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        // POST: Mark all notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notificationService.MarkAllAsReadAsync(userId);
                    TempData["Success"] = "All notifications marked as read.";
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for {User}", GetCurrentUserIdentifier());
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        // POST: Delete notification
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notificationService.DeleteNotificationAsync(id, userId);
                    TempData["Success"] = "Notification deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for {User}", id, GetCurrentUserIdentifier());
                TempData["Error"] = "Failed to delete notification.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}