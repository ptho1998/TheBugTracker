using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Models;

namespace TheBugTracker.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<FileUpload> Uploads { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<Invite> Invites { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<TicketAttachment> Attachments { get; set; }

        public DbSet<TicketComment> Comments { get; set; }

        public DbSet<TicketHistory> TicketHistoty { get; set; }


    }
}
