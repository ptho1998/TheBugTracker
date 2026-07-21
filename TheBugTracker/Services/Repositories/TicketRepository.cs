using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services.Repositories
{
    public class TicketRepository(IDbContextFactory<ApplicationDbContext> contextFactory,
                                  UserManager<ApplicationUser> userManager) : ITicketRepository
    {
        private async Task<bool> CanUserEditTicket(int ticketId, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            bool result = false;

            // if admin -> does the ticket belong to the company?
            if (userInfo.IsInRole(Role.Admin))
            {
                result = await context.Tickets
                    .AnyAsync(t => t.Id == ticketId && t.Project!.CompanyId == userInfo.CompanyId);
            }
            // if PM -> does the ticket belong to one of their projects?
            else if (userInfo.IsInRole(Role.ProjectManager))
            {
                result = await context.Tickets
                    .AnyAsync(t => t.Id == ticketId
                        && (t.Project!.Members.Any(m => m.Id == userInfo.UserId)
                            || t.SubmitterUserId == userInfo.UserId)
                    );
            }
            // otherwise -> are they the submitter or developer?
            else
            {
                result = await context.Tickets
                    .AnyAsync(t => t.Id == ticketId
                        && (t.SubmitterUserId == userInfo.UserId
                            || t.DeveloperUserId == userInfo.UserId)
                    );
            }

            return result;
        }

        public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Ticket> tickets = await context.Tickets
                .Where(t => t.Project!.CompanyId == userInfo.CompanyId
                    && t.Archived == false
                    && t.Status != TicketStatus.Resolved)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.DeveloperUser)
                .ToListAsync();

            return tickets;
        }

        public async Task<IEnumerable<Ticket>> GetResolvedTicketsAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Ticket> tickets = await context.Tickets
                .Where(t => t.Project!.CompanyId == userInfo.CompanyId
                    && t.Archived == false
                    && t.Status == TicketStatus.Resolved)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.DeveloperUser)
                .ToListAsync();

            return tickets;
        }

        public async Task<IEnumerable<Ticket>> GetArchivedTicketsAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Ticket> tickets = await context.Tickets
                .Where(t => t.Project!.CompanyId == userInfo.CompanyId
                    && t.Archived == true)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.DeveloperUser)
                .ToListAsync();

            return tickets;
        }

        public async Task<IEnumerable<Ticket>> GetAssignedTicketsAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<Ticket> tickets = [];

            if (userInfo.IsInRole(Role.ProjectManager))
            {
                List<int> assignedProjectIds = await context.Users
                    .Where(u => u.Id == userInfo.UserId)  // first, get the user
                    .SelectMany(u => u.Projects)  // get a list of their projects
                    .Select(p => p.Id)  // get a list of just the project IDs
                    .ToListAsync();  // pull it out of the DB and into C#

                tickets = await context.Tickets
                    .Include(t => t.Project)
                    .Include(t => t.SubmitterUser)
                    .Include(t => t.DeveloperUser)
                    .Where(t => t.Archived == false)
                    .Where(t => t.SubmitterUserId == userInfo.UserId //the user submitted the ticket
                        || t.DeveloperUserId == userInfo.UserId      //the user is the developer on the ticket
                        || assignedProjectIds.Contains(t.ProjectId)) //the ticket belongs to one of their projects
                    .ToListAsync();
            }
            else
            {
                // get all the tickets they've submitted OR are asssigned to
                tickets = await context.Tickets
                    .Include(t => t.Project)
                    .Include(t => t.SubmitterUser)
                    .Include(t => t.DeveloperUser)
                    .Where(t => t.Archived == false)
                    .Where(t => t.SubmitterUserId == userInfo.UserId
                        || t.DeveloperUserId == userInfo.UserId)
                    .ToListAsync();
            }

            return tickets;
        }

        public async Task<Ticket> CreateTicketAsync(Ticket ticket, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project? project = await context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == ticket.ProjectId && p.CompanyId == userInfo.CompanyId);

            if (project is null)
            {
                throw new ApplicationException("Project does not exist");
            }

            ticket.Status = TicketStatus.New;
            ticket.Created = DateTimeOffset.UtcNow;
            ticket.SubmitterUserId = userInfo.UserId;

            ApplicationUser? developer = null;

            if (!string.IsNullOrEmpty(ticket.DeveloperUserId))
            {
                bool isManagerOfProject = userInfo.IsInRole(Role.ProjectManager) 
                                          && project.Members.Any(m => m.Id == userInfo.UserId);

                if (userInfo.IsInRole(Role.Admin) || isManagerOfProject)
                {
                    developer = project.Members.FirstOrDefault(m => m.Id == ticket.DeveloperUserId);

                    if (developer is not null)
                    {
                        /* check his role */
                        bool isDeveloper = await userManager.IsInRoleAsync(developer, nameof(Role.Developer));

                        if (!isDeveloper) developer = null;
                    }
                }
            }

            ticket.DeveloperUser = developer;
            ticket.DeveloperUserId = developer?.Id;

            context.Add(ticket);
            await context.SaveChangesAsync();

            return ticket;
        }

        public async Task<Ticket?> GetTicketByIdAsync(int id, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Ticket? ticket = await context.Tickets
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.DeveloperUser)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id && t.Project!.CompanyId == userInfo.CompanyId);

            return ticket;
        }

        public async Task ArchiveTicketAsync(int ticketId, UserInfo userInfo)
        {
            if (await CanUserEditTicket(ticketId, userInfo))
            {
                await using ApplicationDbContext context = contextFactory.CreateDbContext();

                Ticket ticket = await context.Tickets
                    .FirstAsync(t => t.Id == ticketId);

                ticket.Archived = true;
                await context.SaveChangesAsync();
            }
        }

        public async Task RestoreTicketAsync(int ticketId, UserInfo userInfo)
        {
            if (await CanUserEditTicket(ticketId, userInfo))
            {
                await using ApplicationDbContext context = contextFactory.CreateDbContext();

                Ticket ticket = await context.Tickets
                    .FirstAsync(t => t.Id == ticketId);

                ticket.Archived = false;
                await context.SaveChangesAsync();
            }
        }

        public async Task UpdateTicketAsync(Ticket ticket, UserInfo userInfo)
        {
            bool canEdit = await CanUserEditTicket(ticket.Id, userInfo);

            if (canEdit)
            {
                ticket.Updated = DateTimeOffset.UtcNow;
                // clear out the navigation properties so EF doesn't try to update them
                ticket.Comments = [];
                ticket.Attachments = [];
                ticket.DeveloperUser = null;
                ticket.SubmitterUser = null;

                await using ApplicationDbContext context = contextFactory.CreateDbContext();
                context.Update(ticket);
                await context.SaveChangesAsync();
            }
        }

        public async Task<TicketComment> CreateCommentAsync(TicketComment comment, UserInfo userInfo)
        {
            bool canEdit = await CanUserEditTicket(comment.TicketId, userInfo);

            if (!canEdit)
            {
                throw new ApplicationException(
                    $"User {userInfo.Email} is not authorized to comment on ticket {comment.TicketId}"
                );
            }

            comment.Created = DateTimeOffset.Now;
            comment.UserId = userInfo.UserId;

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            return comment;
        }

        public async Task UpdateCommentAsync(TicketComment comment, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            if (await context.Comments.AnyAsync(c => c.Id == comment.Id && c.UserId == userInfo.UserId))
            {
                context.Update(comment);
                await context.SaveChangesAsync();
            }
        }

        public async Task<TicketComment?> GetCommentByIdAsync(int id, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            TicketComment? comment = await context.Comments
                .FirstOrDefaultAsync(c => c.Id == id && c.Ticket!.Project!.CompanyId == userInfo.CompanyId);

            return comment;
        }

        public async Task DeleteCommentAsync(int id, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            TicketComment? comment = null;

            if (userInfo.IsInRole(Role.Admin))
            {
                comment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id && c.Ticket!.Project!.CompanyId == userInfo.CompanyId);
            }
            else
            {
                comment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userInfo.UserId);
            }

            if (comment is not null)
            {
                context.Remove(comment);
                await context.SaveChangesAsync();
            }
        }
    }
}
