using Microsoft.EntityFrameworkCore;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services.Repositories
{
    public class NotificationRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : INotificationRepository
    {
        public async Task<IEnumerable<Notification>> GetAllNotificationsAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Notification> notifications = await context.Notifications
                .Where(n => n.RecipientId == userInfo.UserId)
                .Include(n => n.Sender)
                .Include(n => n.Ticket)
                .Include(n => n.Project)
                .ToListAsync();

            return notifications;
        }

        public async Task<IEnumerable<Notification>> GetLatestNotificationsAsync(int count, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Notification> notifications = await context.Notifications
                .Where(n => n.RecipientId == userInfo.UserId) // only the notifications for this user
                .OrderBy(n => n.HasBeenViewed) // order the unread notifications first
                .ThenByDescending(n => n.Created) // then order the most recent first
                .Take(count) // take only the first {count} of notifications
                .Include(n => n.Sender)
                .ToListAsync();

            return notifications;
        }

        public async Task<int> GetUnreadCountAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            int count = await context.Notifications
                // only the notif. for this user AND it has not been viewed
                .Where(n => n.RecipientId == userInfo.UserId && n.HasBeenViewed == false)
                .CountAsync();

            return count;
        }

        public async Task MarkAllNotificationsAsReadAsync(UserInfo userInfo, NotificationType? type = null)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            //await context.Notifications
            //    .Where(n => n.RecipientId == userInfo.UserId && !n.HasBeenViewed)
            //    .Where(n => type == null || n.Type == type)
            //    .ExecuteUpdateAsync(query => query.SetProperty(n => n.HasBeenViewed, true));

            List<Notification> notifications = await context.Notifications
                .Where(n => n.RecipientId == userInfo.UserId && !n.HasBeenViewed)
                .Where(n => type == null || n.Type == type)
                .ToListAsync();

            foreach (Notification notification in notifications)
            {
                notification.HasBeenViewed = true;
            }

            await context.SaveChangesAsync();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Notification? notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId
                                        && n.RecipientId == userInfo.UserId
                                        && !n.HasBeenViewed);

            if (notification is not null)
            {
                notification.HasBeenViewed = true;
                await context.SaveChangesAsync();
            }
        }
    }
}
