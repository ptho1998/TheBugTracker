using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;

namespace TheBugTracker.Models
{
    public class Project
    {
        private DateTimeOffset _created;

        private DateTimeOffset _startDate;

        private DateTimeOffset _endDate;

        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        public DateTimeOffset Created { 
            get => _created; 
            set => _created = value.ToUniversalTime(); 
        }

        public DateTimeOffset StartDate { 
            get => _startDate; 
            set => _startDate = value.ToUniversalTime(); 
        }

        public DateTimeOffset EndDate { 
            get => _endDate; 
            set => _endDate = value.ToUniversalTime(); 
        }

        public ProjectPriority Priority { get; set; }

        public bool Archived { get; set; } = false;

        //navigation properties

        public int CompanyId { get; set; }

        public virtual Company? Company { get; set; }

        public virtual ICollection<ApplicationUser> Members { get; set; } = [];

        public virtual ICollection<Ticket> Tickets { get; set; } = [];

    }

    public static class ProjectExtensions
    {
        public static ProjectDTO ToDTO(this Project project)
        {
            foreach (var ticket in project.Tickets)
            {
            ticket.Project = null;
            }

            ProjectDTO dto = new ()
            {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Created = project.Created,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Priority = project.Priority,
            Archived = project.Archived,
            Tickets = [.. project.Tickets.Select(t => t.ToDTO())],
            Members = [.. project.Members.Select(u => u.ToDTO())]
            };

            return dto;
        }
    }
}
