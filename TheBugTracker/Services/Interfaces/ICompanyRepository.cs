using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;

namespace TheBugTracker.Services.Interfaces
{
    public interface ICompanyRepository
    {
        /// <summary>
        /// Get all users in the current user's company
        /// </summary>
        /// <param name="userInfo">The current user's claims</param>
        Task<IEnumerable<ApplicationUser>> GetUsersAsync(UserInfo userInfo);

        /// <summary>
        /// Get all users in the current user's company
        /// in a specific role
        /// </summary>
        /// <param name="role">The role assigned to the users</param>
        /// <param name="userInfo">The current user's claims</param>
        Task<IEnumerable<ApplicationUser>> GetUsersInRoleAsync(Role role, UserInfo userInfo);

        /// <summary>
        /// Get detailed information about
        /// the current user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The Company object representing the user's company</returns>
        Task<Company> GetCompanyAsync(UserInfo userInfo);

        /// <summary>
        /// Updates a company's details in the database,
        /// if the user is authorized to do so
        /// </summary>
        /// <param name="company">The updated company details</param>
        /// <param name="userInfo">The current user's claims</param>
        Task UpdateCompanyAsync(Company company, UserInfo userInfo);

        /// <summary>
        /// Assigns a user to a new role, removing them from any
        /// previous roles.
        ///
        /// Note: Only admins may reassign user's roles, and only
        /// within their company. Demo Users may never be reassigned.
        /// </summary>
        /// <param name="userId">The ID of the user to assign a new role</param>
        /// <param name="newRole">The role to assign to the user</param>
        /// <param name="userInfo">The current user's claims</param>
        Task AssignUserRole(string userId, Role newRole, UserInfo userInfo);

        /// <summary>
        /// Retrieves the data for the user's company dashboard
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<DashboardDTO> GetDashboardDataAsync(UserInfo userInfo);
    }
}
