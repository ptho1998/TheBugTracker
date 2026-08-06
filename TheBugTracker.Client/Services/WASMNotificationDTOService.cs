using System.Net.Http.Json;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services
{
    public class WASMNotificationDTOService(HttpClient http) : INotificationDTOService
    {
        public async Task<IEnumerable<NotificationDTO>> GetAllNotificationsAsync(UserInfo userInfo)
        {
            try
            {
                List<NotificationDTO> notifications = await http.GetFromJsonAsync<List<NotificationDTO>>("api/notifications")
                    ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<IEnumerable<NotificationDTO>> GetLatestNotificationsAsync(int count, UserInfo userInfo)
        {
            try
            {
                List<NotificationDTO> notifications = await http.GetFromJsonAsync<List<NotificationDTO>>(
                    $"api/notifications/latest?count={count}"
                ) ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<int> GetUnreadCountAsync(UserInfo userInfo)
        {
            try
            {
                string responseBody = await http.GetStringAsync("api/notifications/unread/count");
                int count = int.Parse(responseBody); // "20" -> 20

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(UserInfo userInfo, NotificationType? type = null)
        {
            string query = type.HasValue ? $"?type={type}" : string.Empty;

            var response = await http.DeleteAsync($"api/notifications{query}");
            response.EnsureSuccessStatusCode();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId, UserInfo userInfo)
        {
            var response = await http.DeleteAsync($"api/notifications/{notificationId}");
            response.EnsureSuccessStatusCode();
        }
    }
}
