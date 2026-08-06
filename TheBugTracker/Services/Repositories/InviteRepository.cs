using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services.Repositories
{
    public class InviteRepository : IInviteRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IEmailSender _emailSender;
        private readonly IDataProtector _protector;
        private readonly ICompanyRepository _companyRepository;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;

        public InviteRepository(IDbContextFactory<ApplicationDbContext> contextFactory,
                                IEmailSender emailSender,
                                IDataProtectionProvider protectionProvider,
                                IConfiguration config,
                                ICompanyRepository companyRepository,
                                IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
        {
            _contextFactory = contextFactory;
            _emailSender = emailSender;
            _companyRepository = companyRepository;
            _claimsFactory = claimsFactory;

            string protectionPurpose = config["InviteProtectionKey"]
                ?? throw new ApplicationException("InviteProtectionKey not found in configuration!");

            _protector = protectionProvider.CreateProtector(protectionPurpose);
        }

        public async Task<bool> SendInviteAsync(Uri baseUri, int inviteId, UserInfo userInfo)
        {
            // validate that the user is an admin
            if (userInfo.IsInRole(Role.Admin) == false)
            {
                return false;
            }

            try
            {
                await using ApplicationDbContext context = _contextFactory.CreateDbContext();

                // validate that the invite belongs to their company
                Invite? invite = await context.Invites
                    .Include(i => i.Company)
                    .Include(i => i.Invitor)
                    .Include(i => i.Project)
                    .FirstOrDefaultAsync(i => i.Id == inviteId
                                            && i.CompanyId == userInfo.CompanyId
                                            && i.IsValid == true);

                // validate the invite (ValidateInvite)
                if (invite is null || ValidateInvite(invite) == false)
                {
                    if (invite is not null)
                    {
                        invite.IsValid = false;
                        await context.SaveChangesAsync();
                    }

                    return false;
                }

                // encrypt invitee email, CompanyId, CompanyToken
                string protectedEmail = _protector.Protect(invite.InviteeEmail!);
                string protectedCompanyId = _protector.Protect(invite.CompanyId.ToString());
                string protectedToken = _protector.Protect(invite.CompanyToken.ToString());

                // https://mybugtrackerwebsite.com OR https://localhost:7162
                string baseUrl = baseUri.GetLeftPart(UriPartial.Authority);
                string inviteUrl = $"{baseUrl}/Account/Register/Invite?token={protectedToken}&email={protectedEmail}&company={protectedCompanyId}";

                // construct the email
                string subject = $"You have been invited to join {invite.Company!.Name} on The Bug Tracker!";

                string message = string.IsNullOrEmpty(invite.Message)
                    ? string.Empty
                    : $"""
                        <hr />
                        <p>Message from {invite.Invitor!.FirstName} {invite.Invitor!.LastName}:</p>
                        <blockquote>{invite.Message}</blockquote>
                        <hr />
                        """;

                string body = $"""
                                <h1>Welcome to The Bug Tracker!</h1>
                                <p>
                                    {invite.Invitor!.FirstName} {invite.Invitor!.LastName} has invited you to
                                    join {invite.Company!.Name} on The Bug Tracker to work on the project
                                    {invite.Project!.Name}!
                                </p>
                                {message}
                                <p>
                                    To accept this invitation, please <a href="{inviteUrl}">click here</a>
                                    to register a new account.
                                </p>
                                <p>
                                    <strong>Note:</strong> this invite will expire on {invite.InviteDate.AddDays(7):d}
                                </p>
                                <small>
                                    If you are unable to click the link above, please copy & paste the following URL into
                                    your browser's address bar
                                    <br />
                                    {inviteUrl}
                                </small>
                                """;

                await _emailSender.SendEmailAsync(invite.InviteeEmail!, subject, body);

                return true;

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public async Task CancelInviteAsync(int inviteId, UserInfo userInfo)
        {
            if (!userInfo.IsInRole(Role.Admin))
            {
                return;
            }

            await using ApplicationDbContext context = _contextFactory.CreateDbContext();

            Invite? invite = await context.Invites
                .FirstOrDefaultAsync(i => i.Id == inviteId
                    && i.CompanyId == userInfo.CompanyId
                    && i.IsValid == true);

            if(invite is not null)
            {
                invite.IsValid = false;
                await context.SaveChangesAsync();
            }
        }

        public async Task<Invite> CreateInviteAsync(Invite invite, UserInfo userInfo)
        {
            // invites can only be created by an admin
            if (!userInfo.IsInRole(Role.Admin))
            {
                throw new ApplicationException($"{userInfo.Email} is not authorized to create invites");
            }

            await using ApplicationDbContext context = _contextFactory.CreateDbContext();

            // invites can only be sent to new users
            if (await context.Users.AnyAsync(u => u.Email!.ToUpper() == invite.InviteeEmail!.ToUpper()))
            {
                throw new ApplicationException($"User with email {invite.InviteeEmail} already exists!");
            }

            // invites can only be for projects belonging to the company
            if (!await context.Projects.AnyAsync(p => p.Id == invite.ProjectId && p.CompanyId == userInfo.CompanyId))
            {
                throw new ApplicationException($"Invalid project ID {invite.ProjectId}");
            }

            invite.CompanyToken = Guid.NewGuid();
            invite.CompanyId = userInfo.CompanyId;
            invite.InvitorId = userInfo.UserId;
            invite.InviteDate = DateTimeOffset.UtcNow;
            invite.IsValid = true;
            invite.InviteeId = null;
            invite.JoinDate = null;

            context.Invites.Add(invite);
            await context.SaveChangesAsync();

            return invite;
        }

        public async Task<IEnumerable<Invite>> GetInvitesAsync(UserInfo userInfo)
        {
            await using ApplicationDbContext context = _contextFactory.CreateDbContext();

            List<Invite> invites = await context.Invites
                .Where(i => i.CompanyId == userInfo.CompanyId)
                .Include(i => i.Invitor)
                .Include(i => i.Invitee)
                .Include(i => i.Project)
                .ToListAsync();

            foreach(Invite invite in invites)
            {
                //invalidate any invites that have expired
                invite.IsValid = ValidateInvite(invite);
            }

            //save those changes
            await context.SaveChangesAsync();

            return invites;
        }

        public async Task<Invite?> GetValidInviteAsync(string protectedToken, string protectedEmail, string protectedCompanyId)
        {
            try
            {
                // decrypt the token
                string strToken = _protector.Unprotect(protectedToken);
                Guid token = Guid.Parse(strToken);

                // decrypt the email
                string email = _protector.Unprotect(protectedEmail);

                // decrypt companyId
                string strCompanyId = _protector.Unprotect(protectedCompanyId);
                int companyId = int.Parse(strCompanyId);

                await using ApplicationDbContext context = _contextFactory.CreateDbContext();

                Invite? invite = await context.Invites
                    .Include(i => i.Invitor)
                    .Include(i => i.Company)
                    .Include(i => i.Project)
                    .FirstOrDefaultAsync(i => i.CompanyToken == token
                        && i.CompanyId == companyId
                        && i.InviteeEmail == email
                        && i.IsValid == true);

                if (invite is null)
                {
                    return null;
                }

                bool isValid = ValidateInvite(invite);

                if (isValid == false)
                {
                    // update the DB
                    invite.IsValid = false;
                    await context.SaveChangesAsync();

                    // return null
                    return null;
                }

                // if we got here, the invite must be valid
                // so return it
                return invite;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public async Task AcceptInviteAsync(int inviteId, ApplicationUser invitee)
        {
            await using ApplicationDbContext context = _contextFactory.CreateDbContext();

            Invite? invite = await context.Invites
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.CompanyId == invitee.CompanyId);

            if (invite is not null && ValidateInvite(invite))
            {
                invite.JoinDate = DateTimeOffset.UtcNow;
                invite.InviteeId = invitee.Id;
                invite.IsValid = false;

                // get the invitee's user info
                var claims = await _claimsFactory.CreateAsync(invitee);
                var userInfo = UserInfoHelper.GetUserInfo(claims)!;

                // get the invitee's company's admins
                IEnumerable<ApplicationUser> admins = await _companyRepository.GetUsersInRoleAsync(Role.Admin, userInfo);

                // notify them that they've joined
                foreach (ApplicationUser admin in admins)
                {
                    var notification = new Notification
                    {
                        Title = "Invite Accepted",
                        Message = $"{invitee.FirstName} {invitee.LastName} has accepted their invitation to join the " +
                                  $"company and registered their account",
                        RecipientId = admin.Id,
                        SenderId = invitee.Id,
                        Created = DateTimeOffset.UtcNow,
                        Type = NotificationType.Company,
                    };

                    context.Notifications.Add(notification);
                }

                await context.SaveChangesAsync();
            }
        }

        private bool ValidateInvite(Invite invite)
        {
            bool isValid = invite.IsValid
                && DateTimeOffset.UtcNow < invite.InviteDate.AddDays(7)
                && invite.JoinDate is null
                && string.IsNullOrEmpty(invite.InviteeId);

            return isValid;
        }

    }
}
