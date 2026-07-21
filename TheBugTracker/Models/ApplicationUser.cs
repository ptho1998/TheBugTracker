using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        public Guid? ProfilePictureId { get; set; }

        public virtual FileUpload? ProfilePicture { get; set; }

        public int CompanyId { get; set; }

        public virtual Company? Company { get; set; }

        public virtual ICollection<Project> Projects { get; set; } = [];
    }

    public static class ApplicationUserExtensions
    {
        public static UserDTO ToDTO(this ApplicationUser user)
        {
            UserDTO dto = new ()
            {
                Id = user.Id,
                FirstName = user.FirstName!,
                LastName = user.LastName!,
                ImageUrl = user.ProfilePictureId.HasValue
                    ? $"uploads/{user.ProfilePictureId}"
                    : $"https://api.dicebear.com/10.x/glass/svg?seed={user.FirstName}{user.LastName}"
            };

            return dto;
        }

        public static async Task<UserDTO> ToDTOWithRole(this ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            UserDTO dto = user.ToDTO();

            var roleNames = await userManager.GetRolesAsync(user);

            string? roleName = roleNames
                .Where(rn => rn != nameof(Role.DemoUser))
                .FirstOrDefault();

            bool success = Enum.TryParse<Role>(roleName, out var role);

            if (success)
            {
                dto.Role = role;
            }

            return dto;
        }
    }

}
