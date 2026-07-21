using Microsoft.AspNetCore.Identity;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class TicketDTOService(ITicketRepository repository,
                                  IProjectRepository projectRepository,
                                  UserManager<ApplicationUser> userManager) : ITicketDTOService
    {

        public async Task<IEnumerable<TicketDTO>> GetOpenTicketsAsync(UserInfo userInfo)
        {
            IEnumerable<Ticket> tickets = await repository.GetOpenTicketsAsync(userInfo);

            IEnumerable<TicketDTO> dtos = tickets.Select(t => t.ToDTO());

            return dtos;

        }

        public async Task<IEnumerable<TicketDTO>> GetResolvedTicketsAsync(UserInfo userInfo)
        {
            IEnumerable<Ticket> tickets = await repository.GetResolvedTicketsAsync(userInfo);

            IEnumerable<TicketDTO> dtos = tickets.Select(t => t.ToDTO());

            return dtos;
        }

        public async Task<IEnumerable<TicketDTO>> GetArchivedTicketsAsync(UserInfo userInfo)
        {
            IEnumerable<Ticket> tickets = await repository.GetArchivedTicketsAsync(userInfo);

            IEnumerable<TicketDTO> dtos = tickets.Select(t => t.ToDTO());

            return dtos;
        }

        public async Task<IEnumerable<TicketDTO>> GetAssignedTicketsAsync(UserInfo userInfo)
        {
            IEnumerable<Ticket> tickets = await repository.GetAssignedTicketsAsync(userInfo);

            IEnumerable<TicketDTO> dtos = tickets.Select(t => t.ToDTO());

            return dtos;
        }

        public async Task<TicketDTO> CreateTicketAsync(TicketDTO ticket, UserInfo userInfo)
        {
            Ticket dbTicket = new()
            {
                Title = ticket.Title,
                Description = ticket.Description,
                Created = DateTimeOffset.UtcNow,
                Status = TicketStatus.New,
                SubmitterUserId = userInfo.UserId,
                DeveloperUserId = ticket.DeveloperUserId,
                ProjectId = ticket.ProjectId,
                Priority = ticket.Priority,
                Type = ticket.Type,
            };

            dbTicket = await repository.CreateTicketAsync(dbTicket, userInfo);

            return dbTicket.ToDTO();
        }

        public async Task<TicketDTO?> GetTicketByIdAsync(int id, UserInfo userInfo)
        {
            Ticket? ticket = await repository.GetTicketByIdAsync(id, userInfo);

            if (ticket is null)
            {
                return null;
            }

            TicketDTO dto = ticket.ToDTO();

            if (ticket.SubmitterUser is not null)
            {
                dto.SubmitterUser = await ticket.SubmitterUser.ToDTOWithRole(userManager);
            }

            if (dto.DeveloperUser is not null)
            {
                dto.DeveloperUser.Role = Role.Developer;
            }

            return dto;
        }

        public async Task ArchiveTicketAsync(int ticketId, UserInfo userInfo)
        {
            await repository.ArchiveTicketAsync(ticketId, userInfo);
        }

        public async Task RestoreTicketAsync(int ticketId, UserInfo userInfo)
        {
            await repository.RestoreTicketAsync(ticketId, userInfo);
        }

        public async Task UpdateTicketAsync(TicketDTO ticket, UserInfo userInfo)
        {
            Ticket? dbTicket = await repository.GetTicketByIdAsync(ticket.Id, userInfo);

            if (dbTicket is null)
            {
                return;
            }

            dbTicket.Title = ticket.Title;
            dbTicket.Description = ticket.Description;
            dbTicket.Updated = DateTimeOffset.UtcNow;
            dbTicket.Priority = ticket.Priority;
            dbTicket.Type = ticket.Type;
            dbTicket.Status = ticket.Status;

            //check if user assigns a new developer...
            if (dbTicket.DeveloperUserId != ticket.DeveloperUserId)
            {
                //check if user is allowed
                var projectManager = await projectRepository.GetProjectManagerAsync(dbTicket.ProjectId, userInfo);

                if (projectManager?.Id == userInfo.UserId || userInfo.IsInRole(Role.Admin))
                {
                    var projectMembers = await projectRepository.GetProjectMembersAsync(dbTicket.ProjectId, userInfo);
                    var developer = projectMembers.FirstOrDefault(m => m.Id == ticket.DeveloperUserId);

                    //check if the new developer is assgined to the project is in role of developer
                    if (developer is not null
                        && await userManager.IsInRoleAsync(developer, nameof(Role.Developer)))
                    {
                        dbTicket.DeveloperUserId = ticket.DeveloperUserId;
                        dbTicket.DeveloperUser = developer;
                    }
                    //or unassgined ticket is developeruserid is null
                    else if (string.IsNullOrEmpty(ticket.DeveloperUserId))
                    {
                        dbTicket.DeveloperUserId = null;
                        dbTicket.DeveloperUser = null;
                    }
                }
            }

            await repository.UpdateTicketAsync(dbTicket, userInfo);
        }

        public async Task<TicketCommentDTO> CreateCommentAsync(TicketCommentDTO comment, UserInfo userInfo)
        {
            TicketComment dbComment = new()
            {
                Content = comment.Content,
                TicketId = comment.TicketId,
                UserId = userInfo.UserId,
                Created = DateTimeOffset.UtcNow,
            };

            dbComment = await repository.CreateCommentAsync(dbComment, userInfo);

            return dbComment.ToDTO();
        }

        public async Task UpdateCommentAsync(TicketCommentDTO comment, UserInfo userInfo)
        {
            TicketComment? dbComment = await repository.GetCommentByIdAsync(comment.Id, userInfo);

            if (dbComment is null || dbComment.UserId != userInfo.UserId)
            {
                return;
            }

            dbComment.Content = comment.Content;
            await repository.UpdateCommentAsync(dbComment, userInfo);
        }

        public async Task DeleteCommentAsync(int id, UserInfo userInfo)
        {
            await repository.DeleteCommentAsync(id, userInfo);
        }
    }
}
