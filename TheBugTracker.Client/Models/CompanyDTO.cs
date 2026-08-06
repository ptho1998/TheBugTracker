using System.ComponentModel;
using static MudBlazor.CategoryTypes;

namespace TheBugTracker.Client.Models
{
    public class CompanyDTO
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string ImageUrl { get; set; } = $"https://api.dicebear.com/10.x/glass/svg?seed={Random.Shared.Next()}";

        public ICollection<UserDTO> Members { get; set; } = [];

        public ICollection<ProjectDTO> Projects { get; set; } = [];

        public ICollection<InviteDTO> Invites { get; set; } = [];

    }
}
