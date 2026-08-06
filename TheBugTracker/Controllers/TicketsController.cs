using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheBugTracker.Client.Helpers;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Helpers;
using static MudBlazor.CategoryTypes;

namespace TheBugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController(ITicketDTOService ticketService) : ControllerBase
    {
        private UserInfo UserInfo => UserInfoHelper.GetUserInfo(User)!;

        /// <summary>
        /// Get Tickets
        /// </summary>
        /// <param name="filter">
        /// Optionally filters the tickets by resolved, assigned, or archived tickets.
        /// By default, returns all open tickets.
        /// </param>
        /// <remarks>
        /// Returns a collection of tickets belonging to the user's company. By default,
        /// returns all open tickets. The filter query parameter can be used to retrieve
        /// tickets which are archived, resolved, or assigned to the current user.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDTO>>> GetTickets([FromQuery] TicketsFilter filter = TicketsFilter.Open)
        {
            IEnumerable<TicketDTO> tickets = filter switch
            {
                TicketsFilter.Resolved => await ticketService.GetResolvedTicketsAsync(UserInfo),
                TicketsFilter.Assigned => await ticketService.GetAssignedTicketsAsync(UserInfo),
                TicketsFilter.Archived => await ticketService.GetArchivedTicketsAsync(UserInfo),
                _ => await ticketService.GetOpenTicketsAsync(UserInfo),
            };

            return Ok(tickets);
        }

        /// <summary>
        /// Get Ticket by ID
        /// </summary>
        /// <param name="id">The ID of the ticket to retrieve</param>
        /// <remarks>
        /// Retrieves detailed information about a
        /// specific ticket. Returns 404 Not Found
        /// if no such ticket exists.
        /// </remarks>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDTO?>> GetTicketById([FromRoute] int id)
        {
            TicketDTO? ticket = await ticketService.GetTicketByIdAsync(id, UserInfo);

            if (ticket is null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }

        /// <summary>
        /// Update Ticket
        /// </summary>
        /// <param name="id">The ID of the ticket to update</param>
        /// <param name="ticket">The updated ticket details</param>
        /// <remarks>
        /// Updates a specific ticket, if it exists and the user
        /// is authorized to do so.
        /// </remarks>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTicket([FromRoute] int id, [FromBody] TicketDTO ticket)
        {
            if (id != ticket.Id)
            {
                return BadRequest();
            }

            await ticketService.UpdateTicketAsync(ticket, UserInfo);

            return NoContent();
        }

        /// <summary>
        /// Restore Ticket
        /// </summary>
        /// <param name="id">The ID of the ticket to restore</param>
        /// <remarks>
        /// Un-archives a ticket, if it exists and the user is authorized
        /// to do so.
        /// </remarks>
        [HttpPatch("{id:int}/archive")]
        public async Task<IActionResult> ArchiveTicket([FromRoute] int id)
        {
            await ticketService.ArchiveTicketAsync(id, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Restore Ticket
        /// </summary>
        /// <param name="id">The ID of the ticket to restore</param>
        /// <remarks>
        /// Un-archives a ticket, if it exists and the user is authorized
        /// to do so.
        /// </remarks>
        [HttpPatch("{id:int}/restore")]
        public async Task<IActionResult> RestoreTicket([FromRoute] int id)
        {
            await ticketService.RestoreTicketAsync(id, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Create Ticket
        /// </summary>
        /// <param name="ticket">The ticket to create</param>
        /// <remarks>
        /// Creates a new ticket for the specified project,
        /// if the project exists in the current user's company.
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<TicketDTO>> CreateTicket([FromBody] TicketDTO ticket)
        {
            try
            {
                var createdTicket = await ticketService.CreateTicketAsync(ticket, UserInfo);

                return CreatedAtAction(
                    actionName: nameof(GetTicketById),
                    routeValues: new { id = createdTicket.Id },
                    value: createdTicket);
            }
            catch (ApplicationException invalidProjectException)
            {
                Console.WriteLine(invalidProjectException);
                return BadRequest(invalidProjectException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Problem();
            }
        }

        /// <summary>
        /// Create Comment
        /// </summary>
        /// <param name="ticketId">The ID of the ticket to comment on</param>
        /// <param name="comment">The comment to save</param>
        /// <remarks>
        /// Only the submitter, developer, project manager, or admin associated
        /// with a ticket may submit comments to the ticket.
        /// </remarks>
        [HttpPost("{ticketId:int}/comments"), Tags("Comments")]
        public async Task<ActionResult<TicketCommentDTO>> CreateComment([FromRoute] int ticketId,
                                                                [FromBody] TicketCommentDTO comment)
        {
            if (ticketId != comment.TicketId)
            {
                return BadRequest();
            }

            try
            {
                TicketCommentDTO createdComment = await ticketService.CreateCommentAsync(comment, UserInfo);
                return Ok(createdComment);
            }
            catch (ApplicationException invalidTicketException)
            {
                Console.WriteLine(invalidTicketException);
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Problem();
            }
        }

        /// <summary>
        /// Update Comment
        /// </summary>
        /// <param name="ticketId">The ID of the ticket the comment belongs to</param>
        /// <param name="commentId">The ID of the comment to update</param>
        /// <param name="comment">The update contents of the comment</param>
        /// <remarks>
        /// Updates the content of a specific comment.
        ///
        /// Comments may only be updated by the user who created them.
        /// </remarks>
        [HttpPut("{ticketId:int}/comments/{commentId:int}"), Tags("Comments")]
        public async Task<IActionResult> UpdateComment([FromRoute] int ticketId,
                                               [FromRoute] int commentId,
                                               [FromBody] TicketCommentDTO comment)
        {
            if (comment.TicketId != ticketId || comment.Id != commentId)
            {
                return BadRequest();
            }

            await ticketService.UpdateCommentAsync(comment, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Delete Comment
        /// </summary>
        /// <param name="id">The ID of the comment to delete</param>
        /// <remarks>
        /// Deletes a specific comment, if the user is authorized to do so.
        ///
        /// Only the comment's author or admin may delete a comment.
        /// </remarks>
        [HttpDelete("comments/{id:int}"), Tags("Comments")]
        public async Task<IActionResult> DeleteComment([FromRoute] int id)
        {
            await ticketService.DeleteCommentAsync(id, UserInfo);
            return NoContent();
        }


        /// <summary>
        /// Upload Ticket Attachment
        /// </summary>
        /// <param name="ticketId">The ID of the ticket</param>
        /// <param name="file">The file to attach</param>
        /// <param name="attachment">The attachment's metadata</param>
        /// <remarks>
        /// Uploads a new file attachment to the specified ticket, if
        /// the user is authorized to do so. Users may only upload an attachment
        /// to a ticket when they are one of the following:
        /// - The assigned developer or submitter
        /// - The ticket's assigned Project Manager
        /// - An admin belonging to the ticket's company
        /// </remarks>
        [HttpPost("{ticketId:int}/attachments"), Tags("Ticket Attachments")]
        public async Task<ActionResult<TicketAttachmentDTO>> CreateAttachment([FromRoute] int ticketId,
                                                                              [FromForm(Name = "file")] IFormFile file,
                                                                              [FromForm] TicketAttachmentDTO attachment)
        {
            if (attachment.TicketId != ticketId || file.Length > BrowserFileHelper.MaxFileSize)
            {
                return BadRequest();
            }

            try
            {
                attachment.UserId = UserInfo.UserId;
                attachment.Created = DateTimeOffset.UtcNow;
                attachment.FileName = file.FileName;

                var upload = await UploadHelper.GetFileUploadAsync(file);

                TicketAttachmentDTO? createdAttachment = await ticketService.CreateTicketAttachmentAsync(
                        attachment, upload.Data!, upload.Type!, UserInfo);

                return Ok(createdAttachment);
            }
            catch (IOException ioException)
            {
                Console.WriteLine(ioException);
                return BadRequest(ioException.Message);
            }
            catch (ApplicationException appException)
            {
                Console.WriteLine(appException);
                return BadRequest("Invalid ticket ID");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Problem();
            }

        }

        /// <summary>
        /// Delete Ticket Attachment
        /// </summary>
        /// <param name="id">The ID of the attachment to delete</param>
        /// <remarks>
        /// Deletes a specific ticket attachment, if the user is authorized
        /// to do so. To delete an attachment, the user must be either
        /// the attachment's owner or an admin of the attachment's company
        /// </remarks>
        [HttpDelete("attachments/{id:int}"), Tags("Ticket Attachments")]
        public async Task<IActionResult> DeleteAttachment([FromRoute] int id)
        {
            await ticketService.DeleteTicketAttachmentAsync(id, UserInfo);

            return NoContent();
        }
    }
}
