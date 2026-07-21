using System.ComponentModel.DataAnnotations;

namespace TheBugTracker.Client.Models
{
    public class TicketCommentDTO
    {
        private DateTimeOffset _created;
        public int Id { get; set; }

        [Required]
        public string? Content { get; set; }

        public DateTimeOffset Created
        {
            get => _created;
            set => _created = value.ToUniversalTime();
        }

        public int TicketId { get; set; }

        [Required]
        public string? UserId { get; set; }

        public UserDTO? User { get; set; }
    }
}
