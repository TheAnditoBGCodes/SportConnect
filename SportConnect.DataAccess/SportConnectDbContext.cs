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
                   UserName = "sportconnectadmin",
                   NormalizedUserName = "SPORTCONNECTADMIN",
                   Email = "admin@sportconnect.com",
                   NormalizedEmail = "ADMIN@SPORTCONNECT.COM",
                   EmailConfirmed = true,
                   FullName = "SportConnect Админ",
                   Country = "България",
                   PasswordHash = "AQAAAAIAAYagAAAAEC2djeaOGVbK4PxfKvpPCnAQBruCM0Jqdy0yX9VFwNrCEp0kQp1l4Zed8A2QXlW0gQ==", //8J2x0}p%@*B,7?<%=.{x                   
                   ImageUrl = @"\uploads\admin.jpg",
                   DateOfBirth = "2006-03-27",
                   SecurityStamp = "d92e94ae-9696-409a-8f12-6d12f95be5a4",
                   ConcurrencyStamp = "53b3c2bc-546e-4207-94b0-1f464c123aaa",
               }
            );
            
            builder.Entity<Sport>().HasData(
                new Sport
                {
                    Id = "d0d1c1a1-e6a6-4a2e-a56b-3d516c897101",
                    Name = "Снукър",
                    Description = "Игра с щека и топки",
                    ImageUrl = @"\uploads\sports\snooker.jpg"
                },
                new Sport
                {
                    Id = "3e20a87e-d7d5-45f6-b775-7f394a4b31f1",
                    Name = "Шахмат",
                    Description = "Интелектуален спорт с фигури",
                    ImageUrl = @"\uploads\sports\chess.jpg"
                },
                new Sport
                {
                    Id = "89d1cc17-0a41-47a2-a9c6-4c9f54b3b1a1",
                    Name = "Дартс",
                    Description = "Игра на хвърляне на стрелички",
                    ImageUrl = @"\uploads\sports\darts.jpg"
                },
                new Sport
                {
                    Id = "bf76e9a3-7f2d-4d11-82d2-6b547531ec71",
                    Name = "Тенис",
                    Description = "Спорт с ракети и мрежа",
                    ImageUrl = @"\uploads\sports\tennis.jpg"
                }
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