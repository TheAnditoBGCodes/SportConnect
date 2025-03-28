using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportConnect.Models;

namespace SportConnect.DataAccess
{
    public class SportConnectDbContext : IdentityDbContext<SportConnectUser>
    {
        public SportConnectDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Tournament> Tournaments { get; set; }

        public DbSet<Sport> Sports { get; set; }

        public DbSet<Participation> Participations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Participation>()
                .HasOne(p => p.Tournament)
                .WithMany(t => t.Participations)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sport>().HasIndex(x => x.Description).IsUnique();
            builder.Entity<Sport>().HasIndex(x => x.Name).IsUnique();
        }
    }
}