using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using TheBugTracker.Models;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Components.Account
{
    public class CustomUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, 
                                                  RoleManager<IdentityRole> roleManager,
                                                  IOptions<IdentityOptions> options) 
                : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
    {
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            string profilePictureUrl = user.ProfilePictureId.HasValue
                ? $"/uploads/{user.ProfilePictureId}"
                : $"https://api.dicebear.com/10.x/glass/svg?seed={user.Id}";

            identity.AddClaims([
                new Claim(AppClaimTypes.FirstName, user.FirstName!),
                new Claim(AppClaimTypes.LastName, user.LastName!),
                new Claim(AppClaimTypes.CompanyId, user.CompanyId.ToString()),
                new Claim(AppClaimTypes.ProfilePictureUrl, profilePictureUrl)
            ]);

            return identity;
        }
    }
}
