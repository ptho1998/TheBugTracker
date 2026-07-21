using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;

namespace TheBugTracker.Models
{
    public class TicketComment
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

        public virtual Ticket? Ticket { get; set; }

        [Required]
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

    }

    public static class TicketCommentExtensions
    {
        public static TicketCommentDTO ToDTO(this TicketComment comment)
        {
            TicketCommentDTO dto = new TicketCommentDTO()
            {
                Id = comment.Id,
                Content = comment.Content,
                Created = comment.Created,
                TicketId = comment.TicketId,
                UserId = comment.UserId,
                User = comment.User?.ToDTO(),
            };

            return dto;
        }
    }
}
