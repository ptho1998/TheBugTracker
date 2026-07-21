using System.ComponentModel.DataAnnotations;
using static MudBlazor.CategoryTypes;

namespace TheBugTracker.Client.Models
{
    public class TicketAttachmentDTO
    {
        private DateTimeOffset _created;

        public int Id { get; set; }

        [Required]
        public string? FileName { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset Created
        {
            get => _created;
            set => _created = value.ToUniversalTime();
        }

        public required string AttachmentUrl { get; set; }

        public int TicketId { get; set; }

        [Required]
        public string? UserId { get; set; }

        public UserDTO? User { get; set; }
    }


}
