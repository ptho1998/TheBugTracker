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
    public class CompanyRepository(IDbContextFactory<ApplicationDbContext> contextFactory, 
                                   UserManager<ApplicationUser> userManager) : ICompanyRepository
    {
        public async Task<Company> GetCompanyAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            Company company = await context.Companies
                .Include(c => c.Members)
                .FirstAsync(c => c.Id == userInfo.CompanyId);

            return company;
        }

        public async Task UpdateCompanyAsync(Company company, UserInfo userInfo)
        {
            if (!userInfo.IsInRole(Role.Admin) || company.Id != userInfo.CompanyId)
            {
                return;
            }

            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            FileUpload? oldImage = null;

            // if there's a new image to save ...
            if (company.Image is not null && company.Image.Id != company.ImageId)
            {
                // find the old image, if it exists
                oldImage = await context.Companies
                    .Where(c => c.Id == userInfo.CompanyId)
                    .Select(c => c.Image)
                    .FirstOrDefaultAsync();

                // insert the new image and update the company's FK
                context.Add(company.Image);
                company.ImageId = company.Image.Id;
            }

            // save our changes
            context.Update(company);
            await context.SaveChangesAsync();

            // if there WAS a previous image ...
            if (oldImage is not null)
            {
                // delete it
                context.Remove(oldImage);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            List<ApplicationUser> users = await context.Users
                .Where(u => u.CompanyId == userInfo.CompanyId)
                .ToListAsync();

            return users;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersInRoleAsync(Role role, UserInfo userInfo)
        {
            IEnumerable<ApplicationUser> usersInRole = await userManager.GetUsersInRoleAsync(Enum.GetName(role)!);

            usersInRole = usersInRole.Where(u => u.CompanyId == userInfo.CompanyId);

            return usersInRole;
        }

        public async Task AssignUserRole(string userId, Role newRole, UserInfo userInfo)
        {
            // ensure the current user is an admin, that we're not assigning to the DemoUser role,
            // and that the user is not trying to change their own role
            if (!userInfo.IsInRole(Role.Admin) || newRole == Role.DemoUser || userId == userInfo.UserId)
            {
                return;
            }

            ApplicationUser? userToAssign = await userManager.FindByIdAsync(userId);

            // does the user exist? are they in the same company?
            if (userToAssign?.CompanyId != userInfo.CompanyId)
            {
                return;
            }

            // remove the user from their previous role(s)
            var orginalRoles = await userManager.GetRolesAsync(userToAssign);

            // verify that user is not a demo user and they're not already in the new role
            if (orginalRoles.Any(roleName => roleName == nameof(Role.DemoUser) || roleName == Enum.GetName(newRole)))
            {
                return;
            }

            try
            {
                var removedResult = await userManager.RemoveFromRolesAsync(userToAssign, orginalRoles);

                if (!removedResult.Succeeded)
                {
                    throw new ApplicationException(string.Join(", ", removedResult.Errors.Select(e => e.Description)));
                }

                var addedResult = await userManager.AddToRoleAsync(userToAssign, Enum.GetName(newRole)!);

                if (!addedResult.Succeeded)
                {
                    throw new ApplicationException(string.Join(", ", removedResult.Errors.Select(e => e.Description)));
                }

                var notification = new Notification
                {
                    Title = "Role Changed",
                    Message = $"You have been assigned to the role of {newRole.GetDisplayName()}",
                    RecipientId = userId,
                    SenderId = userInfo.UserId,
                    Created = DateTimeOffset.UtcNow,
                    Type = NotificationType.Company,
                };

                await using ApplicationDbContext context = contextFactory.CreateDbContext();
                context.Add(notification);
                await context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await userManager.AddToRolesAsync(userToAssign, orginalRoles);

                // rethrow the exception so the UI can show an error message
                throw;
            }
        }

        public async Task<DashboardDTO> GetDashboardDataAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = contextFactory.CreateDbContext();

            IQueryable<Ticket> companyTickets = context.Tickets
                    .Where(t => t.Project!.CompanyId == userInfo.CompanyId && !t.Archived);
            IQueryable<Project> companyProjects = context.Projects
                    .Where(p => p.CompanyId == userInfo.CompanyId && !p.Archived);

            int totalProjectCount = await companyProjects.CountAsync();
            int totalTicketCount = await companyTickets.CountAsync();
            int openTicketCount = await companyTickets.Where(t => t.Status != TicketStatus.Resolved).CountAsync();
            int resolvedTicketCount = await companyTickets.Where(t => t.Status == TicketStatus.Resolved).CountAsync();

            var recentlyUpdatedTickets = await companyTickets
                .OrderByDescending(t => t.Updated.HasValue ? t.Updated : t.Created)
                .Include(t => t.DeveloperUser)
                .Take(5)
                .Select(t => t.ToDTO())
                .ToListAsync();

            List<MonthlyTickets> ticketsOverTime = [];
            List<MonthlyTickets> resolvedOverTime = [];

            var now = DateTime.UtcNow;
            
            // loop over the previous 12 months
            for (int monthsAgo = 12; monthsAgo >= 0; monthsAgo--)
            {
                int year = now.Year;
                int month = now.Month - monthsAgo;

                if (month <= 0) //wrap around for previous years
                {
                    year -= 1;
                    month += 12;
                }

                // find the beginning of the month
                var thisMonth = new DateTimeOffset(
                    year,
                    month,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    TimeSpan.Zero);

                // find the end of the month
                var nextMonth = new DateTimeOffset(
                    year: month == 12 ? year + 1 : year,
                    month: month == 12 ? 1 : month + 1,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    TimeSpan.Zero);

                // query the database for tickets created between start & end
                var createdCount = await companyTickets
                    .CountAsync(t => t.Created >= thisMonth && t.Created < nextMonth);

                // query the database for tickets resolved and updated between start & end
                var resolvedCount = await companyTickets
                    .Where(t => t.Status == TicketStatus.Resolved)
                    .CountAsync(t => t.Updated.HasValue
                        ? (t.Updated >= thisMonth && t.Updated < nextMonth)
                        : (t.Created >= thisMonth && t.Created < nextMonth));

                // ignore the first few months if they don't have any data
                if (ticketsOverTime.Count > 0 || resolvedOverTime.Count > 0 || createdCount > 0 || resolvedCount > 0)
                {
                    ticketsOverTime.Add(new MonthlyTickets(thisMonth, createdCount));
                    resolvedOverTime.Add(new MonthlyTickets(thisMonth, resolvedCount));
                }
            }

            List<TicketPriorityCount> ticketsByPriority = await companyTickets
                .GroupBy(t => t.Priority)
                .Select(group => new TicketPriorityCount(group.Key, group.Count()))
                .ToListAsync();

            List<ProjectDTO> recentDeadlines = await companyProjects
                .OrderBy(p => now > p.EndDate ? now - p.EndDate : p.EndDate - now)
                .Take(5)
                .Select(p => p.ToDTO())
                .ToListAsync();

            List<ProjectPriorityCount> projectsByPriority = await companyProjects
                .GroupBy(p => p.Priority)
                .Select(group => new ProjectPriorityCount(group.Key, group.Count()))
                .ToListAsync();

            List<TicketStatusCount> ticketsByStatus = await companyTickets
                .GroupBy(t => t.Status)
                .Select(group => new TicketStatusCount(group.Key, group.Count()))
                .ToListAsync();

            List<ProjectTicketCount> ticketsByProject = await companyTickets
                .GroupBy(t => t.Project!.Name)
                .Select(group => new ProjectTicketCount(group.Key!, group.Count()))
                .ToListAsync();

            DashboardDTO result = new ()
            {
                TotalProjectCount = totalProjectCount,
                TotalTicketCount = totalTicketCount,
                OpenTicketCount = openTicketCount,
                ResolvedTicketCount = resolvedTicketCount,
                RecentlyUpdatedTickets = recentlyUpdatedTickets,
                TicketsOverTime = ticketsOverTime,
                ResolvedOverTime = resolvedOverTime,
                TicketsByPriority = ticketsByPriority,
                RecentDeadlines = recentDeadlines,
                ProjectsByPriority = projectsByPriority,
                TicketsByStatus = ticketsByStatus,
                TicketsByProject = ticketsByProject,
            };

            return result;
        }
    }
}
