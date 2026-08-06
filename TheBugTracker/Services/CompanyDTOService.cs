using Microsoft.AspNetCore.Identity;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Helpers;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class CompanyDTOService(ICompanyRepository repository,
                                   IInviteRepository inviteRepository,
                                   UserManager<ApplicationUser> userManager) : ICompanyDTOService
    {
        public async Task<CompanyDTO> GetCompanyAsync(UserInfo userInfo)
        {
            Company company = await repository.GetCompanyAsync(userInfo);
            company.Invites = [.. await inviteRepository.GetInvitesAsync(userInfo)];

            CompanyDTO dto = company.ToDTO();
            dto.Members.Clear();

            foreach (ApplicationUser user in company.Members)
            {
                UserDTO userWithRole = await user.ToDTOWithRole(userManager);
                dto.Members.Add(userWithRole);
            }

            return dto;
        }

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

        public async Task UpdateCompanyAsync(CompanyDTO company, UserInfo userInfo)
        {
            if (!userInfo.IsInRole(Role.Admin))
            {
                return;
            }

            Company dbCompany = await repository.GetCompanyAsync(userInfo);

            // clear the navigation properties so we don't update them
            dbCompany.Projects = [];
            dbCompany.Members = [];
            dbCompany.Invites = [];

            // update the properties we want to change
            dbCompany.Name = company.Name;
            dbCompany.Description = company.Description;

            if (company.ImageUrl.StartsWith("data:"))
            {
                try
                {
                    dbCompany.Image = UploadHelper.GetFileUpload(company.ImageUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            await repository.UpdateCompanyAsync(dbCompany, userInfo);
        }

        public async Task AssignUserRole(string userId, Role newRole, UserInfo userInfo)
        {
            if (userInfo.IsInRole(Role.Admin))
            {
                await repository.AssignUserRole(userId, newRole, userInfo);
            }
        }

        public async Task<DashboardDTO> GetDashboardDataAsync(UserInfo userInfo)
        {
            DashboardDTO data = await repository.GetDashboardDataAsync(userInfo);
            return data;
        }
    }
}
