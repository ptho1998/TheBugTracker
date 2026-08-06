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

        /// <summary>
        /// Get Company
        /// </summary>
        /// <remarks>
        /// Fetch detailed information about the
        /// current user's company, including the company's
        /// members and invites.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<CompanyDTO>> GetCompany()
        {
            CompanyDTO company = await companyService.GetCompanyAsync(UserInfo);
            return company;
        }

        /// <summary>
        /// Update Company
        /// </summary>
        /// <param name="company">The updated company details</param>
        /// <remarks>
        /// Updates the name, description, and/or image of the current user's company
        ///
        /// **Note:** Only company administrators may update their company
        /// </remarks>
        [HttpPut, Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> UpdateCompany([FromBody] CompanyDTO company)
        {
            await companyService.UpdateCompanyAsync(company, UserInfo);
            return NoContent();
        }

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

        /// <summary>
        /// Assign User Role
        /// </summary>
        /// <param name="id">The ID of the user</param>
        /// <param name="user">The user ID and role to be assigned</param>
        /// <remarks>
        /// Assigns a user to a new role, removing them from their previous role.
        ///
        /// **Note:** Only admins may assign users a new role. Admins cannot
        /// change their own role.
        /// </remarks>
        [HttpPut("users/{id}"), Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> AssignUserRole([FromRoute] string id, [FromBody] UserDTO user)
        {
            if (user.Id != id || user.Role is null)
            {
                return BadRequest();
            }

            await companyService.AssignUserRole(id, user.Role.Value, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Get Dashboard Data
        /// </summary>
        /// <remarks>
        /// Fetch the metrics used in the dashboard for the current user's company.
        ///
        /// *Note: Review the response body for specific details*
        /// </remarks>
        [HttpGet("dashboard")] // GET: api/company/dashboard
        public async Task<ActionResult<DashboardDTO>> GetDashboardData()
        {
            var dashboardDTO = await companyService.GetDashboardDataAsync(UserInfo);
            return Ok(dashboardDTO);
        }
    }
}
