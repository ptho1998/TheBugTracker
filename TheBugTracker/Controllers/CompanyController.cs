using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanyController(ICompanyDTOService companyService) : ControllerBase
    {
        private UserInfo UserInfo => UserInfoHelper.GetUserInfo(User)!;

        /// <summary>Get Users</summary>
        /// <param name="role">
        /// Optionally filters users to a specific role
        /// </param>
        /// <remarks>
        /// Returns a collection of members of the current user's
        /// company. If a role is supplied, only returns members in
        /// that role.
        /// </remarks>
        [HttpGet("users")] // api/company/users?role=developer
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers([FromQuery] Role? role)
        {
            if (role.HasValue)
            {
                IEnumerable<UserDTO> usersInRole = await companyService.GetUsersInRoleAsync(role.Value, UserInfo);
                return Ok(usersInRole);
            }
            else
            {
                IEnumerable<UserDTO> users = await companyService.GetUsersAsync(UserInfo);
                return Ok(users);
            }


        }
    }
}
