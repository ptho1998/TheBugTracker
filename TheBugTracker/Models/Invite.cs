using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

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
        public string? InviteEmail { get; set; }

        [Required]
        public string? InviteeFistName { get; set; }

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
}
