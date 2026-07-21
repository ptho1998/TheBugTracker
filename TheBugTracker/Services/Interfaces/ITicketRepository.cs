using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;

namespace TheBugTracker.Services.Interfaces
{
    public interface ITicketRepository
    {
        /// <summary>
        /// Retrieves all open tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<Ticket>> GetOpenTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all resolved tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<Ticket>> GetResolvedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all archived tickets in the user's company
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<Ticket>> GetArchivedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Retrieves all tickets assigned to the current user. For
        /// project managers, this will retrieve all tickets they've submitted
        /// and all tickets in their assigned projects
        /// </summary>
        /// <param name="userInfo">The claims of the current user</param>
        Task<IEnumerable<Ticket>> GetAssignedTicketsAsync(UserInfo userInfo);

        /// <summary>
        /// Creates a new ticket in the database
        /// </summary>
        /// <param name="ticket">The ticket to be created</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The ticket after being saved to the database</returns>
        Task<Ticket> CreateTicketAsync(Ticket ticket, UserInfo userInfo);

        /// <summary>
        /// Get details of a specific ticket by ID,
        /// if it exists
        /// </summary>
        /// <param name="id">The ID of the ticket to retrieve</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The ticket matching the ID or null if none exists</returns>
        Task<Ticket?> GetTicketByIdAsync(int id, UserInfo userInfo);

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
        /// Updates a ticket's database record,
        /// if the user is authorized to do so
        /// </summary>
        /// <param name="ticket">The ticket details to save</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task UpdateTicketAsync(Ticket ticket, UserInfo userInfo);

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
        Task<TicketComment> CreateCommentAsync(TicketComment comment, UserInfo userInfo);

        /// <summary>
        /// Updates a comment's content in the database,
        /// if it belongs to the current user
        /// </summary>
        /// <param name="comment">The updated comment</param>
        /// <param name="userInfo">The claims of the current user</param>
        Task UpdateCommentAsync(TicketComment comment, UserInfo userInfo);

        /// <summary>
        /// Get a specific TicketComment by its ID,
        /// if it exists in the current user's company
        /// </summary>
        /// <param name="id">The ID of the comment to retrieve</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>
        /// The selected comment, or null if it does not exist or
        /// belongs to a different company
        /// </returns>
        Task<TicketComment?> GetCommentByIdAsync(int id, UserInfo userInfo);

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
