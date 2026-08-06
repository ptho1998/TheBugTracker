using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController(INotificationDTOService notificationService) : ControllerBase
    {
        private UserInfo UserInfo => UserInfoHelper.GetUserInfo(User)!;

        /// <summary>
        /// Get All Notifications
        /// </summary>
        /// <remarks>
        /// Retrieve all notifications for the current user
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetNotifications()
        {
            IEnumerable<NotificationDTO> notifications = await notificationService.GetAllNotificationsAsync(UserInfo);
            return Ok(notifications);
        }

        /// <summary>
        /// Get Latest Notifications
        /// </summary>
        /// <param name="count">The maximum number of notifications to retrieve</param>
        /// <remarks>
        /// Retrieves a fixed number of the latest notifications for the user.
        ///
        /// This endpoint will return the latest _unread_ notifications first, followed
        /// by the latest read notifications.
        /// </remarks>
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetLatestNotifications([FromQuery] int count = 2)
        {
            IEnumerable<NotificationDTO> notifications = await notificationService.GetLatestNotificationsAsync(count, UserInfo);
            return Ok(notifications);
        }

        /// <summary>
        /// Get Unread Count
        /// </summary>
        /// <remarks>
        /// Returns the total number of unread notifications for
        /// the current user
        /// </remarks>
        [HttpGet("unread/count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            int count = await notificationService.GetUnreadCountAsync(UserInfo);
            return Ok(count);
        }

        /// <summary>
        /// Read All Notifications
        /// </summary>
        /// <param name="type">
        /// Optional; if provided, marks only notifications
        /// of this type as read
        /// </param>
        /// <remarks>
        /// Marks all notifications for the current user as read.
        /// If a notification type is provided, marks only
        /// notifications of that type as read.
        /// </remarks>
        [HttpDelete]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] NotificationType? type = null)
        {
            await notificationService.MarkAllNotificationsAsReadAsync(UserInfo, type);
            return NoContent();
        }

        /// <summary>
        /// Read Notification
        /// </summary>
        /// <param name="id">The ID of the notification</param>
        /// <remarks>
        /// Mark a specific notification as read for the current user
        /// </remarks>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> MarkAsReadById([FromRoute] int id)
        {
            await notificationService.MarkNotificationAsReadAsync(id, UserInfo);
            return NoContent();
        }
    }
}
