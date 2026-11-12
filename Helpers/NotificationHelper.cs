using GYM.Data;
using GYM.Models;

namespace GYM.Helpers
{
    public static class NotificationHelper
    {
        // Creates and saves a new notification for a user, with an optional link
        public static async Task SendAsync(ApplicationDbContext context, int userId, string message, string? linkUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }
    }
}