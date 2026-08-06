using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class InviteDTOService(IInviteRepository repository) : IInviteDTOService
    {
        public async Task CancelInviteAsync(int inviteId, UserInfo userInfo)
        {
            if (userInfo.IsInRole(Role.Admin))
            {
                await repository.CancelInviteAsync(inviteId, userInfo);
            }
        }

        public async Task<InviteDTO> CreateInviteAsync(InviteDTO dto, UserInfo userInfo)
        {
            Invite invite = new ()
            {
                InviteDate = DateTimeOffset.UtcNow,
                InviteeEmail = dto.InviteeEmail,
                InviteeFirstName = dto.InviteeFirstName,
                InviteeLastName = dto.InviteeLastName,
                Message = dto.Message,
                CompanyId = userInfo.CompanyId,
                ProjectId = dto.ProjectId!.Value,
                InvitorId = userInfo.UserId,
            };

            Invite createdInvite = await repository.CreateInviteAsync(invite, userInfo);

            return createdInvite.ToDTO();
        }

        public async Task<IEnumerable<InviteDTO>> GetInvitesAsync(UserInfo userInfo)
        {
            IEnumerable<Invite> invites = await repository.GetInvitesAsync(userInfo);
            IEnumerable<InviteDTO> dtos = invites.Select(i => i.ToDTO());

            return dtos;
        }

        public async Task<bool> SendInviteAsync(Uri baseUri, int inviteId, UserInfo userInfo)
        {
            if (userInfo.IsInRole(Role.Admin))
            {
                return await repository.SendInviteAsync(baseUri, inviteId, userInfo);
            }

            return false;
        }
    }
}
