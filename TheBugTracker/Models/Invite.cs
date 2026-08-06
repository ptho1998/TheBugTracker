using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;

namespace TheBugTracker.Models
{
    public class Invite
    {
        private DateTimeOffset _inviteDate;

        private DateTimeOffset? _joinDate;

        public int Id { get; set; }

        public DateTimeOffset InviteDate
        {
            get => _inviteDate;
            set => _inviteDate = value.ToUniversalTime();
        }

        public DateTimeOffset? JoinDate
        {
            get => _joinDate;
            set => _joinDate = value?.ToUniversalTime();
        }

        public Guid CompanyToken { get; set; }

        [Required, EmailAddress]
        public string? InviteeEmail { get; set; }

        [Required]
        public string? InviteeFirstName { get; set; }

        [Required]
        public string? InviteeLastName { get; set; }

        public string? Message { get; set;  }

        public bool IsValid { get; set;  }

        public int CompanyId { get; set; }

        public virtual Company? Company { get; set; }

        public int ProjectId { get; set; }

        public virtual Project? Project { get; set; }

        [Required]
        public string? InvitorId { get; set; }

        public virtual ApplicationUser? Invitor { get; set; }

        public string? InviteeId { get; set; }

        public virtual ApplicationUser? Invitee { get; set; }
    }

    public static class InviteExtensions
    {
        public static InviteDTO ToDTO(this Invite invite)
        {
            InviteDTO dto = new()
            {
                Id = invite.Id,
                InviteDate = invite.InviteDate,
                JoinDate = invite.JoinDate,
                InviteeEmail = invite.InviteeEmail,
                InviteeFirstName = invite.InviteeFirstName,
                InviteeLastName = invite.InviteeLastName,
                Message = invite.Message,
                IsValid = invite.IsValid,
                ProjectId = invite.ProjectId,
                Project = invite.Project?.ToDTO(),
                InvitorId = invite.InvitorId,
                Invitor = invite.Invitor?.ToDTO(),
                InviteeId = invite.InviteeId,
                Invitee = invite.Invitee?.ToDTO(),
            };

            return dto;
        }
    }
}
