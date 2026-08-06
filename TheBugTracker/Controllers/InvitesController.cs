using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitesController(IInviteDTOService inviteService) : ControllerBase
    {
        private UserInfo UserInfo => UserInfoHelper.GetUserInfo(User)!;

        /// <summary>Get Invites</summary>
        /// <remarks>
        /// Gets all invites for the user's company
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InviteDTO>>> GetInvites()
        {
            IEnumerable<InviteDTO> invites = await inviteService.GetInvitesAsync(UserInfo);
            return Ok(invites);
        }

        /// <summary>Create Invite</summary>
        /// <param name="invite">The details of the invite to create</param>
        /// <remarks>
        /// Creates a new invite record in the database for the user's company.
        /// Only admins may create invites.
        ///
        /// **Note**: This endpoint only *creates* an invite, but does not
        /// send the invite to the recipient's email. To send the invite,
        /// use the Send Invite endpoint.
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<ActionResult<InviteDTO>> CreateInvite([FromBody] InviteDTO invite)
        {
            try
            {
                InviteDTO createdInvite = await inviteService.CreateInviteAsync(invite, UserInfo);
                return Ok(createdInvite);
            }
            catch(ApplicationException validateEx)
            {
                Console.WriteLine(validateEx.Message);
                return BadRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        /// <summary>
        /// Send Invite
        /// </summary>
        /// <param name="inviteId">The ID of the invite to send</param>
        /// <remarks>
        /// Sends a previously created invite via email to that invite's
        /// InviteeEmail. Only admins may send invites.
        /// </remarks>
        [HttpPost("{inviteId:int}/send")] // POST: api/invites/24/send
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> SendInvite([FromRoute] int inviteId)
        {
            Uri requestUri = new Uri(Request.GetEncodedUrl());
            Uri baseUri = new Uri(requestUri.GetLeftPart(UriPartial.Authority));

            bool sent = await inviteService.SendInviteAsync(baseUri, inviteId, UserInfo);

            return sent ? NoContent() : BadRequest();
        }

        /// <summary>
        /// Cancel Invite
        /// </summary>
        /// <param name="inviteId">The ID of the invite to cancel</param>
        /// <remarks>
        /// Marks an invite as invalid so that it cannot be used. Only admins
        /// may cancel pending invitations, and only pending invitations may be cancelled.
        /// </remarks>
        [HttpDelete("{inviteId:int}")]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> CancelInvite([FromRoute] int inviteId)
        {
            await inviteService.CancelInviteAsync(inviteId, UserInfo);
            return NoContent();
        }

    }
}
