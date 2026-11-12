using System.Security.Claims;
using GYM.Data;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Fetches the current user's 15 most recent notifications plus unread count
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(15)
                .Select(n => new NotificationItemViewModel
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    LinkUrl = n.LinkUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            int unreadCount = await _context.Notifications.CountAsync(n => n.UserId == CurrentUserId && !n.IsRead);

            return Json(new { notifications, unreadCount });
        }

        // Marks a single notification as read
        public async Task<IActionResult> MarkAsRead([FromBody] NotificationIdPayload payload)
        {
            var notification = await _context.Notifications.FindAsync(payload.NotificationId);
            if (notification == null || notification.UserId != CurrentUserId)
            {
                return Json(new { success = false });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Marks all of the current user's unread notifications as read
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}