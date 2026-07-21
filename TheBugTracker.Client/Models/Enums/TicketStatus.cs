using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TheBugTracker.Client.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TicketStatus
    {
        New,
        [Display(Name = "In Development")] InDevelopment,
        Testing,
        Resolved,
    }
}
