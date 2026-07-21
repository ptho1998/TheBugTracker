using System.Text.Json.Serialization;

namespace TheBugTracker.Client.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Urgent,
    }
}
