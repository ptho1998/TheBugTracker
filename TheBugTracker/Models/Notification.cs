using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Models
{
    public class Notification
    {
        private DateTimeOffset _created;

        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        [Required]
        public string? Message { get; set; }

        public DateTimeOffset Created
        {
            get => _created;
            set => _created = value.ToUniversalTime();
        }

        public NotificationType Type { get; set; }  

        public bool HasBeenViewed { get; set; }

        public int? TicketId { get; set; }

        public virtual Ticket? Ticket { get; set; }

        public int? ProjectId { get; set; }

        public virtual Project? Project { get; set; }

        [Required]
        public string? RecipientId { get; set; }

        public virtual ApplicationUser? Recipient {  get; set; }

        [Required]
        public string? SenderId { get; set; }

        public virtual ApplicationUser? Sender { get; set; }
    }

    public static class NotificationExtensions
    {
        public static NotificationDTO ToDTO(this Notification notification)
        {
            NotificationDTO dto = new NotificationDTO
            {
                Id = notification.Id,
                Title = notification.Title!,
                Message = notification.Message!,
                Created = notification.Created,
                HasBeenViewed = notification.HasBeenViewed,
                Type = notification.Type,
                TicketId = notification.TicketId,
                Ticket = notification.Ticket?.ToDTO(),
                ProjectId = notification.ProjectId,
                Project = notification.Project?.ToDTO(),
                SenderId = notification.SenderId!,
                Sender = notification.Sender?.ToDTO(),
            };

            return dto;
        }
    }
}
