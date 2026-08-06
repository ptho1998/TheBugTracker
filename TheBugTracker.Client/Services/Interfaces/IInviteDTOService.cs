using TheBugTracker.Client.Models;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services.Interfaces
{
    public interface IInviteDTOService
    {
        /// <summary>
        /// Saves a new invite for the user's company
        /// to the database. Only admins may create invites.
        /// </summary>
        /// <param name="invite">The details of the invite to create</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The created invite after being saved to the database</returns>
        Task<InviteDTO> CreateInviteAsync(InviteDTO invite, UserInfo userInfo);

        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The invites for the user's company</returns>
        Task<IEnumerable<InviteDTO>> GetInvitesAsync(UserInfo userInfo);

        /// <summary>
        /// Invalidates a previously created invite, if the
        /// user is authorized to do so
        /// </summary>
        /// <param name="inviteId">The ID of the invite to cancel</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task CancelInviteAsync(int inviteId, UserInfo userInfo);

        /// <summary>
        /// Send a unique invite registration link to the email associated
        /// with the specified invite
        /// </summary>
        /// <param name="baseUri">The base URL to the application</param>
        /// <param name="inviteId">The ID of the invite to send</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>True if the email was sent successfully, or false if there was an error</returns>
        Task<bool> SendInviteAsync(Uri baseUri, int inviteId, UserInfo userInfo);
    }
}
