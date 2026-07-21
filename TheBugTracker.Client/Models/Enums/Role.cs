using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TheBugTracker.Client.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Role
    {
        Admin,
        [Display(Name = "Project Manager")] ProjectManager,
        Developer,
        Submitter,
        [Display(Name = "Demo User")] DemoUser,
    }
}
