using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TheBugTracker.Client.Models
{
    public class InviteDTO
    {
        private DateTimeOffset _inviteDate;

        private DateTimeOffset? _joinDate;

        [Description("The unique identifier for the invite")]
        public int Id { get; set; }

        [Description("The date and time, in UTC, the invite was created")]
        public DateTimeOffset InviteDate
        {
            get => _inviteDate;
            set => _inviteDate = value.ToUniversalTime();
        }

        [Description("The date and time, in UTC, the invite was accepted and a user account was registered.")]
        public DateTimeOffset? JoinDate
        {
            get => _joinDate;
            set => _joinDate = value?.ToUniversalTime();
        }

        [Required(ErrorMessage = "Email Address is required"), EmailAddress(ErrorMessage = "Invalid e-mail address")]
        [Description("The email address to send the invite")]
        public string? InviteeEmail { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [Description("The first name of the person being invited to join the company")]
        public string? InviteeFirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [Description("The last name of the person being invited to join the company")]
        public string? InviteeLastName { get; set; }

        public string InviteeFullName => $"{InviteeFirstName} {InviteeLastName}";

        [Description("An optional message to be sent with the invite")]
        public string? Message { get; set; }

        [Description("Indicates whether the invite is valid to be accepted. Invites will automatically expire after 7 days")]
        public bool IsValid { get; set; }

        [Required(ErrorMessage = "Please select a project")]
        [Description("The ID of the project the user is being invited to join")]
        public int? ProjectId { get; set; }

        [Description("The details of the project the user is being invited to join")]
        public ProjectDTO? Project { get; set; }

        [Required]
        [Description("The ID of the admin who sent the invite")]
        public string? InvitorId { get; set; }

        [Description("The details of the admin who sent the invite")]
        public virtual UserDTO? Invitor { get; set; }

        [Description("The ID of the user who registered with the invite")]
        public string? InviteeId { get; set; }

        [Description("The details of the user who registered with the invite")]
        public virtual UserDTO? Invitee { get; set; }
    }
}
