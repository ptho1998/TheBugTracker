using System.ComponentModel;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Client.Models
{
    public class DashboardDTO
    {
        [Description("The total number of projects in the company")]
        public int TotalProjectCount { get; set; }

        [Description("The total number of tickets in the company")]
        public int TotalTicketCount { get; set; }

        [Description("The total number of tickets not in a resolved status")]
        public int OpenTicketCount { get; set; }

        [Description("The total number of tickets in a resolved status")]
        public int ResolvedTicketCount { get; set; }

        [Description("The five most recently updated tickets in the company")]
        public IEnumerable<TicketDTO> RecentlyUpdatedTickets { get; set; } = [];

        [Description("The number of tickets created each month")]
        public IEnumerable<MonthlyTickets> TicketsOverTime { get; set; } = [];

        [Description("The number of tickets resolved each month")]
        public IEnumerable<MonthlyTickets> ResolvedOverTime { get; set; } = [];

        [Description("The number of tickets assigned to each priority")]
        public IEnumerable<TicketPriorityCount> TicketsByPriority { get; set; } = [];

        [Description("The company projects that are closest to their end date")]
        public IEnumerable<ProjectDTO> RecentDeadlines { get; set; } = [];

        [Description("The number of company projects assigned to each priority")]
        public IEnumerable<ProjectPriorityCount> ProjectsByPriority { get; set; } = [];

        [Description("The number of tickets in the company in each status")]
        public IEnumerable<TicketStatusCount> TicketsByStatus { get; set; } = [];

        [Description("The number of tickets in each of the company's projects")]
        public IEnumerable<ProjectTicketCount> TicketsByProject { get; set; } = [];
    }

    public record MonthlyTickets(DateTimeOffset Month, int Count);

    public record TicketPriorityCount(TicketPriority Priority, int Count);

    public record ProjectPriorityCount(ProjectPriority Priority, int Count);

    public record TicketStatusCount(TicketStatus Status, int Count);

    public record ProjectTicketCount(string ProjectName, int Count);
}
