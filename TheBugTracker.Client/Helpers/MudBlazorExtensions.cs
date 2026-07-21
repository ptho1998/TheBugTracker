using MudBlazor;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Client.Helpers
{
    public static class MudBlazorExtensions
    {
        public static Color GetColor(this ProjectPriority priority)
        {
            Color color = priority switch
            {
                ProjectPriority.Low => Color.Success,
                ProjectPriority.Medium => Color.Secondary,
                ProjectPriority.High => Color.Error,
                ProjectPriority.Urgent => Color.Dark,
                _ => Color.Default
            };

            return color;
        }

        public static Color GetColor(this TicketPriority priority)
        {
            Color color = priority switch
            {
                TicketPriority.Low => Color.Success,
                TicketPriority.Medium => Color.Secondary,
                TicketPriority.High => Color.Error,
                TicketPriority.Urgent => Color.Dark,
                _ => Color.Default
            };

            return color;
        }

        public static Color GetColor(this TicketStatus status)
        {
            Color color = status switch
            {
                TicketStatus.New => Color.Info,
                TicketStatus.InDevelopment => Color.Primary,
                TicketStatus.Testing => Color.Warning,
                TicketStatus.Resolved => Color.Success,
                _ => Color.Default
            };

            return color;
        }

        public static Color GetColor(this TicketType type)
        {
            Color color = type switch
            {
                TicketType.Defect => Color.Error,
                TicketType.Enhancement => Color.Info,
                TicketType.WorkTask => Color.Tertiary,
                TicketType.GeneralTask => Color.Primary,
                TicketType.ChangeRequest => Color.Secondary,
                TicketType.NewDevelopment => Color.Success,
                _ => Color.Default
            };

            return color;
        }
    }
}
