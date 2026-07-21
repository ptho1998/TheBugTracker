using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Client.Models
{
    public class TicketDTO
    {
        private DateTimeOffset _created;

        private DateTimeOffset? _updated;

        [Description("The unique identifier of the ticket")]
        public int Id { get; set; }

        [Description("The title of the ticket")]
        [Required]
        public string? Title { get; set; }

        [Description("The detailed description of the ticket, for example acceptance criteria or task details")]
        [Required]
        public string? Description { get; set; }

        [Description("The date and time the ticket was created in UTC")]
        public DateTimeOffset Created
        {
            get => _created;
            set => _created = value.ToUniversalTime();
        }

        [Description("The date and time the ticket was last updated in UTC")]
        public DateTimeOffset? Updated
        {
            get => _updated;
            set
            {
                _updated = value?.ToUniversalTime();
            }
        }

        [Description("Indicates whether the ticket is currently archived")]
        public bool Archived { get; set; }

        [Description("Indicates whether the ticket is archived as a result of archiving its project")]
        public bool ArchivedByProject { get; set; }

        [Description("The priority level of the ticket")]
        public TicketPriority Priority { get; set; }

        [Description("The type of task described by the ticket")]
        public TicketType Type { get; set; }

        [Description("The current status of the ticket's task")]
        public TicketStatus Status { get; set; }

        //navigation properties

        [Description("The ID of the project the ticket belongs to")]
        [Range(1, int.MaxValue, ErrorMessage ="Please select a valid porject")]
        public int ProjectId { get; set; }

        [Description("The details of the project this ticket belongs to")]
        public ProjectDTO? Project { get; set; }

        [Description("The ID of the user who created this ticket")]
        [Required]
        public string? SubmitterUserId { get; set; }

        [Description("The details of the user who created this ticket")]
        public UserDTO? SubmitterUser { get; set; }

        [Description("The ID of the developer this ticket is assigned to, if any")]
        public string? DeveloperUserId { get; set; }

        [Description("The details of the developer this ticket is assigned to, if any")]
        public UserDTO? DeveloperUser { get; set; }

        [Description("Comments that have been left on this ticket")]
        public ICollection<TicketCommentDTO> Comments { get; set; } = [];

        [Description("Files uploaded in support of this ticket")]
        public ICollection<TicketHistoryDTO> History { get; set; } = [];

        [Description("A history of events related to this ticket")]
        public ICollection<TicketAttachmentDTO> Attachments { get; set; } = [];

        [JsonIgnore]
        public DateTimeOffset LastModified => Updated ?? Created;
    }
}
