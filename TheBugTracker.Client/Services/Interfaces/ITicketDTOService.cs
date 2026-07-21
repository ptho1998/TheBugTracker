using TheBugTracker.Client.Models;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services.Interfaces
{
    public interface ITicketDTOService
    {
        /// <summary>
        /// Retrieves all open tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<TicketDTO>> GetOpenTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all resolved tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<TicketDTO>> GetResolvedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all archived tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<TicketDTO>> GetArchivedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all tickets assigned to the current user. For
        /// project managers, this will retrieve all tickets they've submitted
        /// and all tickets in their assigned projects
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<TicketDTO>> GetAssignedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Save a new ticket to the database
        /// </summary>
        /// <param name="ticket">The details of the newly created ticket</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The created ticket, after it has been assigned an ID</returns>
        Task<TicketDTO> CreateTicketAsync(TicketDTO ticket, UserInfo userInfo);

        /// <summary>
        /// Get details of a specific ticket by ID,
        /// if it exists
        /// </summary>
        /// <param name="id">The ID of the ticket to retrieve</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The ticket matching the ID or null if none exists</returns>
        Task<TicketDTO?> GetTicketByIdAsync(int id, UserInfo userInfo);

        /// <summary>
        /// Archives a ticket, if it is not already archived
        /// and the user is authorized to do so
        /// </summary>
        /// <param name="ticketId">The ID of the ticket to archive</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task ArchiveTicketAsync(int ticketId, UserInfo userInfo);

        /// <summary>
        /// Restores a ticket, if it is currently archived
        /// and the user is authorized to do so
        /// </summary>
        /// <param name="ticketId">The ID of the ticket to restore</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task RestoreTicketAsync(int ticketId, UserInfo userInfo);

        /// <summary>
        /// Updates a ticket in the database
        /// </summary>
        /// <param name="ticket">The updated ticket information</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task UpdateTicketAsync(TicketDTO ticket, UserInfo userInfo);

        /// <summary>
        /// Creates a new comment for the specified ticket,
        /// if the user is authorized to do so
        /// </summary>
        /// <param name="comment">The details of the comment to save</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>
        /// The created comment, after it has been saved to the database
        /// and assigned an ID
        /// </returns>
        Task<TicketCommentDTO> CreateCommentAsync(TicketCommentDTO comment, UserInfo userInfo);

        /// <summary>
        /// Updates a comment's content in the database,
        /// if it belongs to the current user
        /// </summary>
        /// <param name="comment">The updated comment</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task UpdateCommentAsync(TicketCommentDTO comment, UserInfo userInfo);

        /// <summary>
        /// Deletes a comment if it either belongs to the current
        /// user or the current user is an admin and the comment
        /// belongs to their company
        /// </summary>
        /// <param name="id">The ID of the comment to delete</param>
        /// <param name="userInfo">The current user's claims</param>
        Task DeleteCommentAsync(int id, UserInfo userInfo);
    }
}
