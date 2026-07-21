using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Client.UserInfoHelper
{
    public class UserInfo
    {
        public required string UserId { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public required string ProfilePictureUrl { get; set; }
        public required int CompanyId { get; set; }
        public required string[] Roles { get; set; }
    }

    public static class UserInfoExtensions
    {
        public static bool IsInRole(this UserInfo userInfo, Role role)
        {
            string? roleName = Enum.GetName(role);

            if (roleName is null)
            {
                return false;
            }

            return userInfo.Roles.Contains(roleName);
        }
    }
}
