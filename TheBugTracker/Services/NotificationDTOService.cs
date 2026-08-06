using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class NotificationDTOService(INotificationRepository repository) : INotificationDTOService
    {
        public async Task<IEnumerable<NotificationDTO>> GetAllNotificationsAsync(UserInfo userInfo)
        {
            IEnumerable<Notification> notifications = await repository.GetAllNotificationsAsync(userInfo);
            IEnumerable<NotificationDTO> dtos = notifications.Select(n => n.ToDTO());

            return dtos;
        }

        public async Task<IEnumerable<NotificationDTO>> GetLatestNotificationsAsync(int count, UserInfo userInfo)
        {
            var notifications = await repository.GetLatestNotificationsAsync(count, userInfo);
            var dtos = notifications.Select(n => n.ToDTO());

            return dtos;
        }

        public async Task<int> GetUnreadCountAsync(UserInfo userInfo)
        {
            int count = await repository.GetUnreadCountAsync(userInfo);

            return count;
        }

        public async Task MarkAllNotificationsAsReadAsync(UserInfo userInfo, NotificationType? type = null)
        {
            await repository.MarkAllNotificationsAsReadAsync(userInfo, type);
        }

        public async Task MarkNotificationAsReadAsync(int notificationId, UserInfo userInfo)
        {
            await repository.MarkNotificationAsReadAsync(notificationId, userInfo);
        }
    }
}
