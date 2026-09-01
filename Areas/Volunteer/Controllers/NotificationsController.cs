using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using System.Security.Claims;

namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Full notifications page
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // Bell dropdown (called by JS fetch)
        [HttpGet]
        public async Task<IActionResult> GetDropdown()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Json(new
            {
                unreadCount,
                notifications = notifications.Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.IsRead,
                    createdAt = n.CreatedAt.ToString("dd MMM, hh:mm tt")
                })
            });
        }

        // Mark one as read
        [HttpPost]
        public async Task<IActionResult> MarkRead(string notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId
                                       && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Mark all as read
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}