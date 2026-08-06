using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services.Repositories
{
    public class ProjectRepository(IDbContextFactory<ApplicationDbContext> contextFactory,
                                   UserManager<ApplicationUser> userManager,
                                   ICompanyRepository companyRepository) : IProjectRepository
    {
        public async Task<IEnumerable<Project>> GetProjectAsync(UserInfo user)
        {
            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

            IEnumerable<Project> projects = await context.Projects
                .Where(p => p.CompanyId == user.CompanyId && p.Archived == false)
                .Include(p => p.Members)
                .ToListAsync();

            return projects;
        }

        public async Task<IEnumerable<Project>> GetArchivedProjectAsync(UserInfo user)
        {
            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

            IEnumerable<Project> projects = await context.Projects
                .Where(p => p.CompanyId == user.CompanyId && p.Archived == true)
                .Include(p => p.Members)
                .ToListAsync();

            return projects;
        }

        public async Task<IEnumerable<Project>> GetAssignedProjectAsync(UserInfo user)
        {
            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

            IEnumerable<Project> projects = await context.Projects
                .Include(p => p.Members)
                .Where(p => p.CompanyId == user.CompanyId && p.Archived == false)
                .Where(p => user.IsInRole(Role.Admin) || p.Members.Any(m => m.Id == user.UserId))
                .ToListAsync();

            return projects;
        }

        public async Task<Project?> GetProjectByIdAsync(int id, UserInfo user)
        {
            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

            Project? project = await context.Projects
                .Include(p => p.Tickets).ThenInclude(t => t.SubmitterUser)
                .Include(p => p.Tickets).ThenInclude(t => t.DeveloperUser)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == user.CompanyId);

            return project;

        }

        public async Task<Project> CreateProjectAsync(Project project, UserInfo user)
        {
            bool isAdmin  = user.Roles.Any( r => r == nameof(Role.Admin));
            bool isPm = user.Roles.Any(r => r == nameof(Role.ProjectManager));

            if (!isAdmin && !isPm)
            {
                throw new ApplicationException(
                    $"User {user.Email} is not authorized to create a project because they are not an Admin or Project Manager"
                );
            }

            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

            project.CompanyId = user.CompanyId;
            project.Created = DateTimeOffset.UtcNow;

            if (isPm)
            {
                ApplicationUser projectManager = await context.Users.FirstAsync(u => u.Id == user.UserId);
                project.Members.Add(projectManager);
            }

            context.Add(project);

            #region notifications

            // notify company admins + project.Members
            var admins = await companyRepository.GetUsersInRoleAsync(Role.Admin, user);

            List<string> recipientIds = [.. admins.Select(c => c.Id)];

            recipientIds.AddRange(project.Members.Select(m => m.Id));

            foreach (string recipientId in recipientIds.Distinct())
            {
                if (recipientId != user.UserId)
                {
                    var notification = new Notification
                    {
                        Title = "Project Created",
                        Message = $"Project \"{project.Name}\" has been created",
                        Created = DateTimeOffset.UtcNow,
                        RecipientId = recipientId,
                        SenderId = user.UserId,
                        Type = NotificationType.Project,
                    };

                    context.Add(notification);
                }
            }

            #endregion

            await context.SaveChangesAsync();

            return project;

        }

        public async Task UpdateProjectAsync(Project project, UserInfo user)
        {
            // the project must belong to their company
            // they must be an admin or PM
            // if they are a PM, they must be assigned to the project

            bool canEditProject = await CanUserEditProject(project.Id, user);

            if (canEditProject)
            {
                await using ApplicationDbContext context = contextFactory.CreateDbContext();

                // clear out navigation properties so we don't
                // update them by mistake
                project.Members = [];
                project.Tickets = [];

                context.Projects.Update(project);

                #region Notifications
                var projectManager = await GetProjectManagerAsync(project.Id, user);

                if (projectManager is not null && projectManager.Id != user.UserId)
                {
                    var notification = new Notification
                    {
                        Title = "Project Updated",
                        Message = $"Project \"{project.Name}\" has been updated",
                        Created = DateTimeOffset.UtcNow,
                        RecipientId = projectManager.Id,
                        SenderId = user.UserId,
                        Type = NotificationType.Project,
                        ProjectId = project.Id,
                    };

                    context.Add(notification);
                }
                #endregion

                await context.SaveChangesAsync();
            }
        }

        public async Task ArchiveProjectAsync(int projectId, UserInfo user)
        {
            bool canEdit = await CanUserEditProject(projectId, user);

            if (canEdit == false)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project project = await context.Projects
                .Include(p => p.Tickets)
                .FirstAsync(p => p.Id == projectId && p.CompanyId == user.CompanyId);

            project.Archived = true;

            foreach(Ticket ticket in project.Tickets)
            {
                // if ticket.Archived == true, then the ticket was archived by a user
                // if ticket.Archived == false, then the ticket will be archived by the project
                ticket.ArchivedByProject = !ticket.Archived;
                ticket.Archived = true;

                ticket.History.Add(new TicketHistory()
                {
                    UserId = user.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Description = "Project archived"
                });
            }

            #region Notifications
            // notify all project members
            foreach (ApplicationUser member in project.Members)
            {
                if (member.Id != user.UserId)
                {
                    var notification = new Notification
                    {
                        Title = "Project Archived",
                        Message = $"Project \"{project.Name}\" has been archived",
                        RecipientId = member.Id,
                        SenderId = user.UserId,
                        Type = NotificationType.Project,
                        ProjectId = project.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Add(notification);
                }
            }
            #endregion

            await context.SaveChangesAsync();

        }

        public async Task RestoreProjectAsync(int projectId, UserInfo user)
        {
            bool canEdit = await CanUserEditProject(projectId, user);

            if (canEdit == false)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project project = await context.Projects
                .Include(p => p.Tickets)
                .FirstAsync(p => p.Id == projectId && p.CompanyId == user.CompanyId);

            project.Archived = false;

            foreach (Ticket ticket in project.Tickets)
            {
                // if ArchivedByProject == true, the ticket should no longer be archived
                // if ArchivedByProject == false, the ticket was archived before the project and so should remain archived
                ticket.Archived = !ticket.ArchivedByProject;
                ticket.ArchivedByProject = false;

                ticket.History.Add(new TicketHistory()
                {
                    UserId = user.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Description = "Project restored"
                });
            }

            #region Notifications
            // notify all project members
            foreach (ApplicationUser member in project.Members)
            {
                if (member.Id != user.UserId)
                {
                    var notification = new Notification
                    {
                        Title = "Project Restored",
                        Message = $"Project \"{project.Name}\" has been restored from the archive",
                        RecipientId = member.Id,
                        SenderId = user.UserId,
                        Type = NotificationType.Project,
                        ProjectId = project.Id,
                        Created = DateTimeOffset.UtcNow,
                    };

                    context.Add(notification);
                }
            }
            #endregion

            await context.SaveChangesAsync();
        }

        public async Task AddProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            // is this user allowed to assign members to this project?
            bool canEditProject = await CanUserEditProject(projectId, user);

            if (!canEditProject)
            {
                return;
            }
            
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project project = await context.Projects
                .Include(p => p.Members)
                .FirstAsync(p => p.Id == projectId);

            // are they already assigned this project?
            if (project.Members.Any(m => m.Id == userId))
            {
                return;
            }

            // does the user exist?
            // do they belong to the same company?
            ApplicationUser? newMember = await context.Users
                .Where(u => u.CompanyId == user.CompanyId)
                .FirstOrDefaultAsync(u => u.Id == userId);
           
            if(newMember is null 
                || await userManager.IsInRoleAsync(newMember, nameof(Role.ProjectManager)) 
                || await userManager.IsInRoleAsync(newMember, nameof(Role.Admin)))
            {
                return;
            }

            // assign the user to the project and save to the DB
            project.Members.Add(newMember);

            #region Notifications
            // notify newMember
            var memberNotification = new Notification
            {
                Title = "Assigned to Project",
                Message = $"You have been assigned to a new project: {project.Name}",
                RecipientId = userId,
                SenderId = user.UserId,
                Created = DateTimeOffset.UtcNow,
                Type = NotificationType.Project,
                ProjectId = projectId,
            };

            context.Add(memberNotification);

            // if this is done by an admin, notify the PM
            if (user.IsInRole(Role.Admin))
            {
                ApplicationUser? projectManager = await GetProjectManagerAsync(projectId, user);
                if (projectManager != null)
                {
                    var pmNotification = new Notification
                    {
                        Title = "New Member Assigned to Project",
                        Message = $"{newMember.FirstName} {newMember.LastName} has been assigned to your project: {project.Name}",
                        RecipientId = projectManager.Id,
                        SenderId = user.UserId,
                        Created = DateTimeOffset.UtcNow,
                        Type = NotificationType.Project,
                        ProjectId = projectId,
                    };

                    context.Add(pmNotification);
                }
            }
            #endregion

            await context.SaveChangesAsync();
        }

        public async Task RemoveProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            // is this user allowed to remove members from this project?
            bool canEditProject = await CanUserEditProject(projectId, user);

            if (!canEditProject)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project project = await context.Projects
                .Include(p => p.Members)
                .FirstAsync(p => p.Id == projectId);

            ApplicationUser? member = project.Members.FirstOrDefault(m => m.Id == userId);

            if(member is null
                || await userManager.IsInRoleAsync(member, nameof(Role.ProjectManager)))
            {
                return;
            }

            project.Members.Remove(member);

            #region Notifications
            // notify the removed member
            var memberNotification = new Notification
            {
                Title = "Removed from Project",
                Message = $"You have been removed from the following project: {project.Name}",
                RecipientId = member.Id,
                SenderId = user.UserId,
                Created = DateTimeOffset.UtcNow,
                Type = NotificationType.Project,
                ProjectId = projectId,
            };

            context.Add(memberNotification);

            // if removed by an admin, notify the PM
            if (user.IsInRole(Role.Admin))
            {
                ApplicationUser? projectManager = await GetProjectManagerAsync(projectId, user);

                if (projectManager != null)
                {
                    var pmNotification = new Notification
                    {
                        Title = "Member Removed from Project",
                        Message = $"{member.FirstName} {member.LastName} has been removed from your project {project.Name}",
                        RecipientId = projectManager.Id,
                        SenderId = user.UserId,
                        Created = DateTimeOffset.UtcNow,
                        Type = NotificationType.Project,
                        ProjectId = projectId,
                    };

                    context.Add(pmNotification);
                }
            }
            #endregion

            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetProjectMembersAsync(int projectId, UserInfo user)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<ApplicationUser> members = await context.Projects
                .Where(p => p.Id == projectId && p.CompanyId == user.CompanyId)
                .SelectMany(p => p.Members)
                .ToListAsync();

            return members;

        }

        public async Task<ApplicationUser?> GetProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            IEnumerable<ApplicationUser> members = await GetProjectMembersAsync(projectId, userInfo);

            foreach (var member in members)
            {
                if (await userManager.IsInRoleAsync(member, nameof(Role.ProjectManager)) == true)
                {
                    return member;
                }
            }

            return null;
        }

        public async Task AssignProjectManagerAsync(int projectId, string managerId, UserInfo userInfo)
        {
            if(await CanUserEditProject(projectId, userInfo) == false)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            ApplicationUser? newManager = await context.Users
                .FirstOrDefaultAsync(u => u.Id == managerId && u.CompanyId == userInfo.CompanyId);

            // does the user exist and are they a project manager
            if (newManager is null
                || await userManager.IsInRoleAsync(newManager, nameof(Role.ProjectManager)) == false)
            {
                //if not, there's nothing to do
                return;
            }

            Project project = await context.Projects
                .Include(p => p.Members)
                .FirstAsync(p => p.Id == projectId && p.CompanyId == userInfo.CompanyId);

            ApplicationUser? oldManager = await GetProjectManagerAsync(projectId, userInfo);

            if (oldManager?.Id == newManager.Id)
            {
                return;
            }
            else if (oldManager is not null)
            {
                ApplicationUser userToRemove = project.Members.First(m  => m.Id == oldManager.Id);
                project.Members.Remove(userToRemove);

                // notify the old PM
                var oldPmNotification = new Notification
                {
                    Title = "Project Unassigned",
                    Message = $"You have been removed as the project manager of {project.Name}",
                    RecipientId = userToRemove.Id,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Project,
                    ProjectId = projectId,
                };

                context.Add(oldPmNotification);
            }

            project.Members.Add(newManager);

            // notify the new PM
            var newPmNotification = new Notification
            {
                Title = "New Project Assigned",
                Message = $"You have been assigned as the Project Manager for a new project: {project.Name}",
                RecipientId = managerId,
                SenderId = userInfo.UserId,
                Created = DateTimeOffset.UtcNow,
                Type = NotificationType.Project,
                ProjectId = projectId,
            };

            context.Add(newPmNotification);

            await context.SaveChangesAsync();
        }

        public async Task RemoveProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            if (await CanUserEditProject(projectId, userInfo) == false)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Project project = await context.Projects
                .Include(p => p.Members)
                .FirstAsync(p => p.Id == projectId && p.CompanyId == userInfo.CompanyId);

            foreach (var member in project.Members)
            {
                if (await userManager.IsInRoleAsync(member, nameof(Role.ProjectManager)) == true)
                {
                    project.Members.Remove(member);

                    var notification = new Notification
                    {
                        Title = "Project Unassigned",
                        Message = $"You have been removed as the Project Manager of {project.Name}",
                        RecipientId = member.Id,
                        SenderId = userInfo.UserId,
                        Created = DateTimeOffset.UtcNow,
                        Type = NotificationType.Project,
                        ProjectId = projectId,
                    };

                    context.Add(notification);

                    await context.SaveChangesAsync();

                    return;
                }
            }
        }

        /// <summary>
        /// Checks that the project exists, that it belongs to the user's
        /// company, and that the user is either an admin or the project
        /// manager assigned to the project
        /// </summary>
        /// <param name="projectId">The ID of the project to check</param>
        /// <param name="userInfo">The claims of the user to authorize</param>
        /// <returns>True if the user is authorized to edit the project, or false if they are not</returns>
        private async Task<bool> CanUserEditProject(int projectId, UserInfo userInfo)
        {
            bool isAdmin = userInfo.Roles.Any(r => r == nameof(Role.Admin));
            bool isPM = userInfo.Roles.Any(r => r == nameof(Role.ProjectManager));

            // is the user an admin or PM?
            if (!isAdmin && !isPM)
            {
                return false;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            bool canEditProject = await context.Projects
                // the project must exist and belong to their company
                .Where(p => p.Id == projectId && p.CompanyId == userInfo.CompanyId)
                // if they're a PM, they must be assigned to the project
                .AnyAsync(p => isAdmin || p.Members.Any(m => m.Id == userInfo.UserId));

            return canEditProject;

            // the project must belong to their company
            // they must be an admin or PM
            // if they're a PM, they must be assigned to the project
        }

    }
}
