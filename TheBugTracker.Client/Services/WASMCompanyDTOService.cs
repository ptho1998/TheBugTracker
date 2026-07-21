using System.Net.Http.Json;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services
{
    public class WASMCompanyDTOService(HttpClient http) : ICompanyDTOService
    {
        public async Task<IEnumerable<UserDTO>> GetUsersAsync(UserInfo userInfo)
        {
            try
            {
                List<UserDTO> users = await http.GetFromJsonAsync<List<UserDTO>>("api/company/users") ?? [];

                return users;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<IEnumerable<UserDTO>> GetUsersInRoleAsync(Role role, UserInfo userInfo)
        {
            try
            {
                List<UserDTO> users = await http.GetFromJsonAsync<List<UserDTO>>($"api/company/users?role={role}") ?? [];

                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }
    }
}
