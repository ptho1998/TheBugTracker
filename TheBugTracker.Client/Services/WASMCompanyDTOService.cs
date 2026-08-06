using System.Net.Http.Json;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services
{
    public class WASMCompanyDTOService(HttpClient http) : ICompanyDTOService
    {
        public async Task<CompanyDTO> GetCompanyAsync(UserInfo userInfo)
        {
            CompanyDTO company = await http.GetFromJsonAsync<CompanyDTO>("api/company")
                ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

            return company;
        }

        public async Task UpdateCompanyAsync(CompanyDTO company, UserInfo userInfo)
        {
            var response = await http.PutAsJsonAsync("api/company", company);
            response.EnsureSuccessStatusCode();
        }

        public async Task AssignUserRole(string userId, Role newRole, UserInfo userInfo)
        {
            UserDTO dto = new()
            {
                Id = userId,
                Role = newRole,
                FirstName = "firstName",
                LastName = "lastName",
            };

            var response = await http.PutAsJsonAsync($"api/company/users/{userId}", dto);
            response.EnsureSuccessStatusCode();
        }

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

        public async Task<DashboardDTO> GetDashboardDataAsync(UserInfo userInfo)
        {
            DashboardDTO data = await http.GetFromJsonAsync<DashboardDTO>("api/Company/dashboard")
                ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

            return data;
        }
    }
}
