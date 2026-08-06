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
                                  UserManager<ApplicationUser> userManager,
                                  IProjectRepository projectRepository) : ITicketRepository
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

            #region Ticket History
            TicketHistory createdEvent = new()
            {
                Created = ticket.Created,
                UserId = userInfo.UserId,
                Description = "Ticket submitted"
            };

            ticket.History.Add(createdEvent);

            if (ticket.DeveloperUser is not null)
            {
                TicketHistory assignedEvent = new()
                {
                    Created = ticket.Created,
                    UserId = userInfo.UserId,
                    Description = $"Ticket assigned to {ticket.DeveloperUser.FirstName} {ticket.DeveloperUser.LastName}"
                };

                ticket.History.Add(assignedEvent);
            }
            #endregion

            #region Notifications
            //notify the PM
            ApplicationUser? projectManager = await projectRepository.GetProjectManagerAsync(ticket.ProjectId, userInfo);

            if (projectManager is not null && projectManager.Id != userInfo.UserId)
            {
                Notification pmNotification = new()
                {
                    Title = "New Ticket",
                    Message = $"A new ticket has been submitted for your project: {project.Name}",
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    Ticket = ticket,
                    RecipientId = projectManager.Id,
                    SenderId = userInfo.UserId,
                };

                context.Notifications.Add(pmNotification);
            }

            // notify the developer, if one was assigned
            if (ticket.DeveloperUser is not null)
            {
                Notification developerNotification = new()
                {
                    Title = "New Ticket Assigned",
                    Message = $"You have been assigned to a new ticket: {ticket.Title}",
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    Ticket = ticket,
                    RecipientId = ticket.DeveloperUserId,
                    SenderId = userInfo.UserId,
                };

                context.Notifications.Add(developerNotification);
            }
            #endregion

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
                .Include(t => t.Attachments)
                    .ThenInclude(a => a.User)
                .Include(t => t.History)
                    .ThenInclude(h => h.User)
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

                #region History
                TicketHistory archivedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Description = "Ticket archived"
                };

                ticket.History.Add(archivedEvent);
                #endregion

                #region Notifications
                // notify the submitter
                if (ticket.SubmitterUserId != userInfo.UserId)
                {
                    var submitterNotification = new Notification
                    {
                        Title = "Ticket Archived",
                        Message = $"Your ticket \"{ticket.Title}\" has been archived",
                        RecipientId = ticket.SubmitterUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = ticket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Notifications.Add(submitterNotification);
                }

                // notify the developer, if any
                if (!string.IsNullOrEmpty(ticket.DeveloperUserId) && ticket.DeveloperUserId != userInfo.UserId)
                {
                    var developerNotification = new Notification
                    {
                        Title = "Ticket Archived",
                        Message = $"A ticket assigned to you has been archived: \"{ticket.Title}\"",
                        RecipientId = ticket.DeveloperUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = ticket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Notifications.Add(developerNotification);
                }

                #endregion

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

                #region History
                TicketHistory restoreEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Description = "Ticket restored from archive"
                };

                ticket.History.Add(restoreEvent);
                #endregion

                #region Notifications
                // notify the submitter
                if (ticket.SubmitterUserId != userInfo.UserId)
                {
                    var submitterNotification = new Notification
                    {
                        Title = "Ticket Restored",
                        Message = $"Your ticket \"{ticket.Title}\" has been restored",
                        RecipientId = ticket.SubmitterUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = ticket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Notifications.Add(submitterNotification);
                }

                // notify the developer, if any
                if (!string.IsNullOrEmpty(ticket.DeveloperUserId) && ticket.DeveloperUserId != userInfo.UserId)
                {
                    var developerNotification = new Notification
                    {
                        Title = "Ticket Restored",
                        Message = $"A ticket assigned to you has been restored: \"{ticket.Title}\"",
                        RecipientId = ticket.DeveloperUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = ticket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Notifications.Add(developerNotification);
                }

                #endregion

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

                List<TicketHistory> newHistory = await CreateTicketHistory(ticket, userInfo);

                await using ApplicationDbContext context = contextFactory.CreateDbContext();

                context.Tickets.Update(ticket);
                context.TicketHistoty.AddRange(newHistory);

                await context.SaveChangesAsync();
            }
        }

        private async Task<List<TicketHistory>> CreateTicketHistory(Ticket newTicket, UserInfo userInfo)
        {
            // get the current/old record from the database
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Ticket? oldTicket = await context.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == newTicket.Id && userInfo.CompanyId == t.Project!.CompanyId);

            if (oldTicket is null)
            {
                return [];
            }

            // check the properties we care about against the updated ticket
            // for each changed property, create TicketHistory entry for them
            List<TicketHistory> events = [];

            if (oldTicket.Status != newTicket.Status)
            {
                TicketHistory statusChangedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    TicketId = newTicket.Id,
                    Description = $"Status changed from {oldTicket.Status.GetDisplayName()} to {newTicket.Status.GetDisplayName()}"
                };

                events.Add(statusChangedEvent);
            }

            if (oldTicket.Type != newTicket.Type)
            {
                TicketHistory typeChangedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    TicketId = newTicket.Id,
                    Description = $"Type changed from {oldTicket.Type.GetDisplayName()} to {newTicket.Type.GetDisplayName()}"
                };

                events.Add(typeChangedEvent);
            }

            if (oldTicket.Priority != newTicket.Priority)
            {
                TicketHistory priorityChangedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    TicketId = newTicket.Id,
                    Description = $"Priority changed from {oldTicket.Priority.GetDisplayName()} to {newTicket.Priority.GetDisplayName()}"
                };

                events.Add(priorityChangedEvent);
            }

            if (oldTicket.Description != newTicket.Description || oldTicket.Title != newTicket.Title)
            {
                TicketHistory descriptionChangedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    TicketId = newTicket.Id,
                    Description = "Title or description updated"
                };

                events.Add(descriptionChangedEvent);
            }

            if (oldTicket.DeveloperUserId != newTicket.DeveloperUserId)
            {
                TicketHistory developerChangedEvent = new()
                {
                    UserId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    TicketId = newTicket.Id,
                };

                // notify the old developer that they were removed, if there is one
                if (!string.IsNullOrEmpty(oldTicket.DeveloperUserId))
                {
                    var oldDevNotification = new Notification
                    {
                        Title = "Ticket Unassigned",
                        Message = $"You have been unassigned from the ticket \"{oldTicket.Title}\"",
                        RecipientId = oldTicket.DeveloperUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = oldTicket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Add(oldDevNotification);
                }

                if (string.IsNullOrEmpty(newTicket.DeveloperUserId))
                {
                    developerChangedEvent.Description = "Ticket unassigned";
                }
                else
                {
                    string? developerName = await context.Users
                        .Where(u => u.Id == newTicket.DeveloperUserId && u.CompanyId == userInfo.CompanyId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefaultAsync();

                    developerChangedEvent.Description = $"Ticket assigned to {developerName ?? "a new developer"}";

                    // notify the new developer that they were assigned
                    var newDevNotification = new Notification
                    {
                        Title = "Ticket Assigned",
                        Message = $"You have been assigned a new ticket: {newTicket.Title}",
                        RecipientId = newTicket.DeveloperUserId,
                        SenderId = userInfo.UserId,
                        Type = NotificationType.Ticket,
                        TicketId = newTicket.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Add(newDevNotification);
                }

                events.Add(developerChangedEvent);
            }
            else if (events.Count > 0 
                    && !string.IsNullOrEmpty(newTicket.DeveloperUserId) 
                    && newTicket.DeveloperUserId != userInfo.UserId) 
            {
                // the ticket was assigned to a developer and the ticket has been changed,
                // but the developer did not change
                var devNotification = new Notification
                {
                    Title = "Ticket Updated",
                    Message = $"A ticket you are assigned has been updated: \"{newTicket.Title}\"",
                    RecipientId = newTicket.DeveloperUserId,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    TicketId = oldTicket.Id,
                };

                context.Add(devNotification);
            }

            // notify the submitter if there were changes
            if (events.Count > 0 && oldTicket.SubmitterUserId != userInfo.UserId)
            {
                var submitterNotification = new Notification
                {
                    Title = "Ticket Updated",
                    Message = $"Your ticket \"{newTicket.Title}\" has been updated",
                    RecipientId = oldTicket.SubmitterUserId,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    TicketId = oldTicket.Id,
                };

                context.Add(submitterNotification);
            }

            await context.SaveChangesAsync();

            return events;
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

            #region Notifications
            // notify: ticket submitter, developer, and anyone who has commented
            List<string> recipientIds = await context.Comments
                .Where(c => c.TicketId == comment.TicketId)
                .Select(c => c.UserId!)
                .ToListAsync();

            Ticket ticket = await context.Tickets.FirstAsync(t => t.Id == comment.TicketId);

            recipientIds.Add(ticket.SubmitterUserId!);

            if (!string.IsNullOrEmpty(ticket.DeveloperUserId))
            {
                recipientIds.Add(ticket.DeveloperUserId!);
            }

            foreach (string recipientId in recipientIds.Distinct())
            {
                if (recipientId != userInfo.UserId)
                {
                    var notification = new Notification
                    {
                        Title = "Comment Added to Ticket",
                        Message = $"{userInfo.FullName} has left a new comment on {ticket.Title}",
                        RecipientId = recipientId,
                        SenderId = userInfo.UserId,
                        Created = DateTimeOffset.Now,
                        Type = NotificationType.Ticket,
                        TicketId = comment.TicketId,
                    };

                    context.Notifications.Add(notification);
                }
            }

            #endregion

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

        public async Task<TicketAttachment> CreateTicketAttachmentAsync(TicketAttachment attachment, UserInfo userInfo)
        {
            bool canUpload = await CanUserEditTicket(attachment.TicketId, userInfo);

            if (canUpload == false)
            {
                throw new ApplicationException(
                    $"User {userInfo.Email} is not authorized to upload attachments for ticket {attachment.TicketId}"
                );
            }

            attachment.Created = DateTimeOffset.UtcNow;
            attachment.UserId = userInfo.UserId;

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            context.Add(attachment);

            #region Notifications
            Ticket ticket = await context.Tickets.FirstAsync(t => t.Id == attachment.TicketId);

            // notify the submitter
            if (ticket.SubmitterUserId != userInfo.UserId)
            {
                var submitterNotification = new Notification
                {
                    Title = "Attachment Added to Ticket",
                    Message = $"{userInfo.FullName} uploaded {attachment.FileName} to your ticket {ticket.Title}",
                    RecipientId = ticket.SubmitterUserId,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    TicketId = ticket.Id,
                };

                context.Add(submitterNotification);
            }

            // notify the developer, if there is one
            if (!string.IsNullOrEmpty(ticket.DeveloperUserId) && ticket.DeveloperUserId != userInfo.UserId)
            {
                var developerNotification = new Notification
                {
                    Title = "Attachment Added to Ticket",
                    Message = $"{userInfo.FullName} uploaded {attachment.FileName} to your ticket {ticket.Title}",
                    RecipientId = ticket.DeveloperUserId,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Ticket,
                    TicketId = ticket.Id,
                };

                context.Add(developerNotification);
            }
            #endregion

            await context.SaveChangesAsync();

            return attachment;
        }

        public async Task DeleteTicketAttachmentAsync(int id, UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            TicketAttachment? attachment;

            if (userInfo.IsInRole(Role.Admin))
            {
                attachment = await context.Attachments
                    .Include(a => a.Upload)
                    .FirstOrDefaultAsync(a => a.Id == id && a.Ticket!.Project!.CompanyId == userInfo.CompanyId);
            }
            else
            {
                attachment = await context.Attachments
                    .Include(a => a.Upload)
                    .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userInfo.UserId);
            }

            if (attachment is not null)
            {
                context.Remove(attachment);
                context.Remove(attachment.Upload!);

                await context.SaveChangesAsync();
            }
        }
    }
}
