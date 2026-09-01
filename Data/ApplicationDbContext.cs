using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Models;
using System.Composition;
using System.Reflection.Emit; //by WW

namespace OpenHandsVolunteerPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Volunteer> Volunteers { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Application> Applications { get; set; }
        //by ANGEL
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OrganizationRating> OrganizationRatings { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Report> Reports { get; set; }

        //by WW
        public DbSet<CreditScoreHistory> CreditScoreHistories { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //by ANGEL
            // Primary keys
            builder.Entity<Follow>().HasKey(f => f.FollowId);
            builder.Entity<OrganizationRating>().HasKey(r => r.RatingId);
            builder.Entity<Notification>().HasKey(n => n.NotificationId);
            builder.Entity<Certificate>().HasKey(c => c.CertificateId);
            builder.Entity<Report>().HasKey(r => r.ReportId);


            // STOP cascade from User → Volunteer
            builder.Entity<Volunteer>()
                .HasOne(v => v.User)
                .WithOne(u => u.Volunteer) //PREVENT volid in ASPNETUSER (1-1 relation)
                .HasForeignKey<Volunteer>(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // STOP cascade from User → Organization
            builder.Entity<Organization>()
                .HasOne(o => o.User)
                .WithOne(u => u.Organization) //PREVENT orgid in ASPNETUSER (1-1 relation)
                .HasForeignKey<Organization>(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // Restrict delete for Volunteer → Applications
            // delete a volunteer fails if they still have applications
            builder.Entity<Application>()
                .HasOne(a => a.Volunteer)
                .WithMany(v => v.Applications)
                .HasForeignKey(a => a.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascade delete for Opportunity → Applications
            builder.Entity<Application>()
                .HasOne(a => a.Opportunity)
                .WithMany(o => o.Applications)
                .HasForeignKey(a => a.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cascade delete for Organization → Opportunities
            builder.Entity<Opportunity>()
                .HasOne(o => o.Organization)
                .WithMany(org => org.Opportunities)
                .HasForeignKey(o => o.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            //by ANGEL
            // Follow relationships
            builder.Entity<Follow>()
                .HasOne(f => f.Volunteer)
                .WithMany(v => v.Follows)
                .HasForeignKey(f => f.VolunteerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Follow>()
                .HasOne(f => f.Organization)
                .WithMany(o => o.Followers)
                .HasForeignKey(f => f.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate follows (one volunteer can only follow an org once)
            builder.Entity<Follow>()
                .HasIndex(f => new { f.VolunteerId, f.OrganizationId })
                .IsUnique();

            // Rating relationships
            builder.Entity<OrganizationRating>()
                .HasOne(r => r.Volunteer)
                .WithMany(v => v.Ratings)
                .HasForeignKey(r => r.VolunteerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrganizationRating>()
                .HasOne(r => r.Organization)
                .WithMany(o => o.Ratings)
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate ratings per opportunity
            builder.Entity<OrganizationRating>()
                .HasIndex(r => new { r.VolunteerId, r.OpportunityId })
                .IsUnique();

            // Notification — no cascade issue, just UserId
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Report — reporter is nullable-safe
            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Certificate>()
                .HasOne(c => c.Volunteer)
                .WithMany()
                .HasForeignKey(c => c.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cycle

            builder.Entity<Certificate>()
                .HasOne(c => c.Organization)
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cycle

            //By WW
            // Credit Score History relationships
            builder.Entity<CreditScoreHistory>()
                .HasOne(h => h.Volunteer)
                .WithMany(v => v.CreditScoreHistories) // One Volunteer has many credit score changes
                .HasForeignKey(h => h.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict); // Keep history even if volunteer is deleted

            builder.Entity<CreditScoreHistory>()
                .HasOne(h => h.Opportunity)
                .WithMany() // Opportunity doesn't need a list of credit histories
                .HasForeignKey(h => h.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict); // Keep history even if opportunity is deleted

            builder.Entity<CreditScoreHistory>()
                .HasOne(h => h.Organization)
                .WithMany() // Organization doesn't need a list of credit histories
                .HasForeignKey(h => h.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict); // Keep history even if organization is deleted
        }
    }

}
