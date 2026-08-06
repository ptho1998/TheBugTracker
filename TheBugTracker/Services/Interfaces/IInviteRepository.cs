using TheBugTracker.Client.Models;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;

namespace TheBugTracker.Services.Interfaces
{
    public interface IInviteRepository
    {
        /// <summary>
        /// Saves a new invite for the user's company
        /// to the database. Only admins may create invites.
        /// </summary>
        /// <param name="invite">The details of the invite to create</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The created invite after being saved to the database</returns>
        Task<Invite> CreateInviteAsync(Invite invite, UserInfo userInfo);

        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The invites for the user's company</returns>
        Task<IEnumerable<Invite>> GetInvitesAsync(UserInfo userInfo);

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

        /// <summary>
        /// Finds a valid invite given the associated protected token,
        /// email, and company ID if it exists and is valid
        /// </summary>
        /// <param name="protectedToken">The invite's protected CompanyToken</param>
        /// <param name="protectedEmail">The invite's protected InviteeEmail</param>
        /// <param name="protectedCompanyId">The invite's protected CompanyId</param>
        /// <returns>
        /// A valid invite, if one exists for the provided values. Returns null if
        /// no invite exists or the invite is no longer valid.
        /// </returns>
        Task<Invite?> GetValidInviteAsync(string protectedToken, string protectedEmail, string protectedCompanyId);

        /// <summary>
        /// Accepts and invalidates an invite on behalf of an invitee
        /// </summary>
        /// <param name="inviteId">The ID of the invite to accept</param>
        /// <param name="invitee">The user registered with the invite</param>
        Task AcceptInviteAsync(int inviteId, ApplicationUser invitee);
    }
}
