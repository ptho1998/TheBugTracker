using Microsoft.AspNetCore.Identity;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class CompanyDTOService(ICompanyRepository repository, 
                                  UserManager<ApplicationUser> userManager) : ICompanyDTOService
    {
        public async Task<IEnumerable<UserDTO>> GetUsersAsync(UserInfo userInfo)
        {
            IEnumerable<ApplicationUser> users = await repository.GetUsersAsync(userInfo);

            List<UserDTO> dtos = [];

            foreach (ApplicationUser user in users) 
            {
                UserDTO dto = await user.ToDTOWithRole(userManager);
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<UserDTO>> GetUsersInRoleAsync(Role role, UserInfo userInfo)
        {
            var usersInRole = await repository.GetUsersInRoleAsync(role, userInfo);

            List<UserDTO> dtos = [];

            foreach (ApplicationUser user in usersInRole)
            {
                UserDTO dto = user.ToDTO();
                dto.Role = role;

                dtos.Add(dto);
            }

            return dtos;
        }
    }
}
