using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace TheBugTracker.Client.UserInfoHelper
{
    public static class UserInfoHelper
    {
        public static string? GetUserId(this ClaimsPrincipal user)
            => user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public static string? GetEmail(this ClaimsPrincipal user)
            => user.FindFirst(ClaimTypes.Email)?.Value;

        public static string? GetFirstName(this ClaimsPrincipal user)
            => user.FindFirst(AppClaimTypes.FirstName)?.Value;

        public static string? GetLastName(this ClaimsPrincipal user)
            => user.FindFirst(AppClaimTypes.LastName)?.Value;

        public static string? GetPictureUrl(this ClaimsPrincipal user)
            => user.FindFirst(AppClaimTypes.ProfilePictureUrl)?.Value;

        public static int? GetCompanyId(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirst(AppClaimTypes.CompanyId)?.Value, out var id)
                ? id : null;

        public static string[] GetRoles(this ClaimsPrincipal user)
            => user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();


        //UserInfo comes from:
        // - Task<Authentication>
        // - Authentication State
        // - Claims Principle

        public static async Task<UserInfo?> GetUserInfoAsync(Task<AuthenticationState>? autStateTask)
        {
            if (autStateTask is null) return null;

            AuthenticationState authState = await autStateTask;

            return GetUserInfo(authState.User);
        }

        public static UserInfo? GetUserInfo(AuthenticationState authState)
        {
            ClaimsPrincipal user = authState.User;
            return GetUserInfo(user);
        }

        public static UserInfo? GetUserInfo(ClaimsPrincipal user)
        {
            try
            {
                return new UserInfo
                {
                    UserId = user.GetUserId() ?? "",
                    Email = user.GetEmail() ?? "",
                    FirstName = user.GetFirstName() ?? "",
                    LastName = user.GetLastName() ?? "",
                    ProfilePictureUrl = user.GetPictureUrl() ?? "",
                    CompanyId = user.GetCompanyId() ?? 0,
                    Roles = user.GetRoles(),
                };
            }
            catch
            {
                return null;
            }
        }
 

    }
}