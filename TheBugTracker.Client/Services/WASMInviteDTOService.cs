using System.Net.Http.Json;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services
{
    public class WASMInviteDTOService(HttpClient http) : IInviteDTOService
    {
        public async Task<InviteDTO> CreateInviteAsync(InviteDTO invite, UserInfo userInfo)
        {
            var response = await http.PostAsJsonAsync("api/invites", invite);
            response.EnsureSuccessStatusCode();

            InviteDTO createdInvite = await response.Content.ReadFromJsonAsync<InviteDTO>()
                ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

            return createdInvite;
        }

        public async Task<bool> SendInviteAsync(Uri baseUri, int inviteId, UserInfo userInfo)
        {
            try
            {
                var response = await http.PostAsync($"api/invites/{inviteId}/send", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public async Task CancelInviteAsync(int inviteId, UserInfo userInfo)
        {
            var response = await http.DeleteAsync($"api/invites/{inviteId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<InviteDTO>> GetInvitesAsync(UserInfo userInfo)
        {
            IEnumerable<InviteDTO> invites = await http.GetFromJsonAsync<List<InviteDTO>>("api/invites")
                ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

            return invites;
        }
    }
}
