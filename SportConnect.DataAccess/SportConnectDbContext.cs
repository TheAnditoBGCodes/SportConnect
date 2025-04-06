using Microsoft.AspNetCore.Identity;
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

            builder.Entity<Participation>().HasKey(p => new { p.ParticipantId, p.TournamentId });
            
            builder.Entity<SportConnectUser>().HasData(
               new SportConnectUser
               {
                   Id = "8ba73947-ec7f-47b7-bb5e-5eae5c217b5b",
                   UserName = "andyfrozen2006",
                   Email = "andrianiliev28@gmail.com",
                   EmailConfirmed = true,
                   NormalizedUserName = "ANDYFROZEN2006",
                   NormalizedEmail = "ANDRIANILIEV28@GMAIL.COM",
                   FullName = "Андриян Илиев",
                   Country = "България",
                   PasswordHash = "AQAAAAIAAYagAAAAEC2djeaOGVbK4PxfKvpPCnAQBruCM0Jqdy0yX9VFwNrCEp0kQp1l4Zed8A2QXlW0gQ==", //8J2x0}p%@*B,7?<%=.{x
                   ImageUrl = @"\uploads\admin.jpg",
                   DateOfBirth = "2006-03-27",
                   SecurityStamp = "d92e94ae-9696-409a-8f12-6d12f95be5a4",
                   ConcurrencyStamp = "53b3c2bc-546e-4207-94b0-1f464c123aaa",
               }
            );

            builder.Entity<Sport>().HasData(
                new Sport { Id = "d0d1c1a1-e6a6-4a2e-a56b-3d516c897101", Name = "Снукър", Description = "Игра на прецизност и стратегия със щека и топки", ImageUrl = @"\uploads\sports\snooker.jpg" },
                new Sport { Id = "bcb52d80-d07a-4b2d-b5b0-bc83987fbf8e", Name = "Фехтовка", Description = "Дуел със саби и бърза реакция", ImageUrl = @"\uploads\sports\fencing.jpg" },
                new Sport { Id = "ed559b0b-45f3-419d-9fe1-1beceac85b44", Name = "Лека атлетика", Description = "Най-основната форма на спорт – бягане, скокове и хвърляния", ImageUrl = @"\uploads\sports\athletics.jpg" },
                new Sport { Id = "2b0ff87f-1212-4c5b-8c3f-bcfb7fe3e5bc", Name = "Скално катерене", Description = "Изкачване на вертикални повърхности с техника и сила", ImageUrl = @"\uploads\sports\climbing.jpg" },
                new Sport { Id = "3e20a87e-d7d5-45f6-b775-7f394a4b31f1", Name = "Шах", Description = "Интелектуален спорт на стратегия и логика", ImageUrl = @"\uploads\sports\chess.jpg" }
            );

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = "Администратор",
                    NormalizedName = "АДМИНИСТРАТОР"
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = "Потребител",
                    NormalizedName = "ПОТРЕБИТЕЛ"
                }
            );

            builder.Entity<IdentityUserRole<string>>().HasKey(p => new { p.RoleId, p.UserId });

            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "1",
                    UserId = "8ba73947-ec7f-47b7-bb5e-5eae5c217b5b",
                }
            );
        }
    }
}