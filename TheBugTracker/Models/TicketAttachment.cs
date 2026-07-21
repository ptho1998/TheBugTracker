using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;

namespace TheBugTracker.Models
{
    public class TicketAttachment
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

        public Guid UploadId { get; set; }

        public FileUpload? Upload { get; set; }

        public int TicketId { get; set; }

        public virtual Ticket? Ticket { get; set; }


        [Required]
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }

    public static class TicketAttachmentExtensions
    {
        public static TicketAttachmentDTO ToDTO(this TicketAttachment attachment)
        {
            TicketAttachmentDTO dto = new()
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                Description = attachment.Description,
                Created = attachment.Created,
                AttachmentUrl = $"api/attachments/{attachment.UploadId}",
                UserId = attachment.UserId,
                User = attachment.User?.ToDTO(),
                TicketId = attachment.TicketId,
            };

            return dto;
        }
    }
}
