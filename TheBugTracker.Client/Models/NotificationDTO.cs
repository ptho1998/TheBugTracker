using System.ComponentModel;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Client.Models
{
    public class NotificationDTO
    {
        private DateTimeOffset _created;

        [Description("The unique identifier for a notification")]
        public int Id { get; set; }

        [Description("The short title of a notification")]
        public required string Title { get; set; }

        [Description("The message content of a notification")]
        public required string Message { get; set; }

        [Description("The date and time the notification was created")]
        public DateTimeOffset Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }

        [Description("Indicates whether the notification is related to a ticket, project, or the user's company")]
        public NotificationType Type { get; set; }

        [Description("True if the user has read or interacted with this notification; otherwise false")]
        public bool HasBeenViewed { get; set; }

        [Description("The ID of the ticket related to this notification, if the notification's Type is Ticket")]
        public int? TicketId { get; set; }

        [Description("The related ticket, if the notification's Type is Ticket")]
        public TicketDTO? Ticket { get; set; }

        [Description("The ID of the related project, if the notification's Type is Project")]
        public int? ProjectId { get; set; }

        [Description("The related project, if the notification's Type is Project")]
        public ProjectDTO? Project { get; set; }

        [Description("The ID of the user whose action created a notification")]
        public required string SenderId { get; set; }

        [Description("The user whose action created a notification")]
        public UserDTO? Sender { get; set; }
    }
}
