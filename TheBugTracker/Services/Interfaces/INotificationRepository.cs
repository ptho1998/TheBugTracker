using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;

namespace TheBugTracker.Services.Interfaces
{
    public interface INotificationRepository
    {
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>All of a user's notifications</returns>
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(UserInfo userInfo);

        /// <summary>
        /// Get a limited number of the latest notifications for a user
        /// </summary>
        /// <param name="count">The maximum number of notifications to retrieve</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<Notification>> GetLatestNotificationsAsync(int count, UserInfo userInfo);

        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The number of unread notifications for a given user</returns>
        Task<int> GetUnreadCountAsync(UserInfo userInfo);

        /// <summary>
        /// Marks a specific notification as viewed
        /// </summary>
        /// <param name="notificationId">The ID of the notification</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task MarkNotificationAsReadAsync(int notificationId, UserInfo userInfo);

        /// <summary>
        /// Marks all notifications as read. If <paramref name="type"/> is not null,
        /// only marks all notifications with that NotificationType as read.
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        /// <param name="type">Optional. If specified, marks only notifications of this type as read</param>
        Task MarkAllNotificationsAsReadAsync(UserInfo userInfo, NotificationType? type = null);
    }
}
