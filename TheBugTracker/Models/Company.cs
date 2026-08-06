using System.ComponentModel.DataAnnotations;
using TheBugTracker.Client.Models;

namespace TheBugTracker.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public Guid? ImageId { get; set; }

        public virtual FileUpload? Image {  get; set; }

        public virtual ICollection<ApplicationUser> Members { get; set; } = [];

        public virtual ICollection<Project> Projects { get; set; } = [];

        public virtual ICollection<Invite> Invites { get; set; } = [];
    }

    public static class CompanyExtensions
    {
        public static CompanyDTO ToDTO(this Company company)
        {
            CompanyDTO dto = new CompanyDTO()
            {
                Name = company.Name,
                Description = company.Description,
                ImageUrl = company.ImageId.HasValue
                    ? $"api/uploads/{company.ImageId}"
                    : $"https://api.dicebear.com/10.x/glass/svg?seed={company.Name}",
                Projects = [.. company.Projects.Select(p => p.ToDTO())],
                Members  = [.. company.Members.Select(u => u.ToDTO())],
                Invites  = [.. company.Invites.Select(i => i.ToDTO())]
            };
            return dto;
        }
    }
}
