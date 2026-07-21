using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services.Interfaces
{
    public interface ICompanyDTOService
    {
        /// <summary>
        /// Get all users in the current user's company
        /// </summary>
        /// <param name="userInfo">The current user's claims</param>
        Task<IEnumerable<UserDTO>> GetUsersAsync(UserInfo userInfo);

        /// <summary>
        /// Get all users in the current user's company
        /// in a specific role
        /// </summary>
        /// <param name="role">The role assigned to the users</param>
        /// <param name="userInfo">The current user's claims</param>
        Task<IEnumerable<UserDTO>> GetUsersInRoleAsync(Role role, UserInfo userInfo);
    }
}
