using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TheBugTracker.Client.Models.Enums;
using static MudBlazor.CategoryTypes;

namespace TheBugTracker.Client.Models
{
    public class UserDTO
    {
        [Description("A unique identifier for the user")]
        public required string Id { get; set; }

        [Description("The user's first name")]
        public required string FirstName { get; set; }

        [Description("The user's last name")]
        public required string LastName { get; set; }

        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";

        [Description("A URL to an image representing this user's profile picture")]
        public string ImageUrl { get; set; } = $"https://api.dicebear.com/10.x/glass/svg?seed={Random.Shared.Next()}";

        [Description("The user's assigned role in the company")]
        public Role? Role { get; set; }
    }


}
