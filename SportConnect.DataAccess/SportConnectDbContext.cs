using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportConnect.Models;

namespace SportConnect.DataAccess
{
    public class SportConnectDbContext : IdentityDbContext<SportConnectUser>
    {
        public SportConnectDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Sport> Sports { get; set; }
        public DbSet<Participation> Participations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Composite key for Participation
            builder.Entity<Participation>().HasKey(p => new { p.ParticipantId, p.TournamentId });

            // Relationships
            builder.Entity<Participation>()
                .HasOne(p => p.Tournament)
                .WithMany(t => t.Participations)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sport name and description must be unique
            builder.Entity<Sport>().HasIndex(x => x.Name).IsUnique();
            builder.Entity<Sport>().HasIndex(x => x.Description).IsUnique();

            // Seed roles
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

            // Seed admin user
            var adminId = "8ba73947-ec7f-47b7-bb5e-5eae5c217b5b";

            builder.Entity<SportConnectUser>().HasData(
                new SportConnectUser
                {
                    Id = adminId,
                    UserName = "sportconnectadmin",
                    NormalizedUserName = "SPORTCONNECTADMIN",
                    Email = "admin@sportconnect.com",
                    NormalizedEmail = "ADMIN@SPORTCONNECT.COM",
                    EmailConfirmed = true,
                    FullName = "SportConnect Админ",
                    Country = "България",
                    PasswordHash = "AQAAAAIAAYagAAAAEC2djeaOGVbK4PxfKvpPCnAQBruCM0Jqdy0yX9VFwNrCEp0kQp1l4Zed8A2QXlW0gQ==",
                    ImageUrl = @"\uploads\admin.jpg",
                    DateOfBirth = "2006-03-27",
                    SecurityStamp = "d92e94ae-9696-409a-8f12-6d12f95be5a4",
                    ConcurrencyStamp = "53b3c2bc-546e-4207-94b0-1f464c123aaa",
                }
            );

            // Assign admin role
            builder.Entity<IdentityUserRole<string>>().HasKey(p => new { p.RoleId, p.UserId });
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "1",
                    UserId = adminId
                }
            );

            var chessId = "3e20a87e-d7d5-45f6-b775-7f394a4b31f1";
            var dartsId = "89d1cc17-0a41-47a2-a9c6-4c9f54b3b1a1";
            var tennisId = "bf76e9a3-7f2d-4d11-82d2-6b547531ec71";

            builder.Entity<Sport>().HasData(
                new Sport
                {
                    Id = chessId,
                    Name = "Шахмат",
                    Description = "Интелектуален спорт с фигури",
                    ImageUrl = @"\uploads\sports\chess.jpg"
                },
                new Sport
                {
                    Id = dartsId,
                    Name = "Дартс",
                    Description = "Игра на хвърляне на стрелички",
                    ImageUrl = @"\uploads\sports\darts.jpg"
                },
                new Sport
                {
                    Id = tennisId,
                    Name = "Тенис",
                    Description = "Спорт с ракети и мрежа",
                    ImageUrl = @"\uploads\sports\tennis.jpg"
                }
            );

            builder.Entity<Tournament>().HasData(
                new Tournament
                {
                    Id = "3a3cf81f-5f85-4ce3-bc99-45c14555a774",
                    Name = "World Chess Championship",
                    Description = "Световното първенство по шахмат",
                    OrganizerId = adminId,
                    Date = "2025-06-10T10:00:00",
                    Deadline = "2025-05-29T23:59:59",
                    Country = "САЩ",
                    ImageUrl = @"\uploads\tournaments\chess\worldchampionship.jpg",
                    SportId = chessId
                },
                new Tournament
                {
                    Id = "a264b51b-f2a4-48fd-b97b-26ff9fdb2d03",
                    Name = "Candidates Tournament",
                    Description = "Престижен турнир по шахмат",
                    OrganizerId = adminId,
                    Date = "2025-09-05T10:00:00",
                    Deadline = "2025-08-01T23:59:59",
                    Country = "Германия",
                    ImageUrl = @"\uploads\tournaments\chess\candidates.jpg",
                    SportId = chessId
                },
                new Tournament
                {
                    Id = "cf5d37f2-c2b2-4b34-9a20-246aaf114fa1",
                    Name = "World Cup",
                    Description = "Световната купа по дартс",
                    OrganizerId = adminId,
                    Date = "2025-07-01T14:00:00",
                    Deadline = "2025-06-01T23:59:59",
                    Country = "Нидерландия",
                    ImageUrl = @"\uploads\tournaments\darts\worldcup.jpg",
                    SportId = dartsId
                },
                new Tournament
                {
                    Id = "51f7e725-2827-4a5d-8b5f-83ad11979f4e",
                    Name = "World Darts Championship",
                    Description = "Най-престижният турнир по дартс",
                    OrganizerId = adminId,
                    Date = "2025-12-15T14:00:00",
                    Deadline = "2025-11-15T23:59:59",
                    Country = "Обединено кралство",
                    ImageUrl = @"\uploads\tournaments\darts\pdcchampionship.jpg",
                    SportId = dartsId
                },
                new Tournament
                {
                    Id = "98aa004e-0937-4f1f-85a3-17bd4f77a250",
                    Name = "Roland Garros",
                    Description = "Вторият турнир от Големия шлем",
                    OrganizerId = adminId,
                    Date = "2025-06-26T11:00:00",
                    Deadline = "2025-05-27T23:59:59",
                    Country = "Франция",
                    ImageUrl = @"\uploads\tournaments\tennis\rolandgarros.jpg",
                    SportId = tennisId
                },
                new Tournament
                {
                    Id = "c3b5f2a3-56d4-4dbb-91a6-ff6f5b6b8659",
                    Name = "Wimbledon",
                    Description = "Най-старият турнир по тенис в света",
                    OrganizerId = adminId,
                    Date = "2025-07-01T11:00:00",
                    Deadline = "2025-06-10T23:59:59",
                    Country = "Обединено кралство",
                    ImageUrl = @"\uploads\tournaments\tennis\wimbledon.jpg",
                    SportId = tennisId
                }
            );
        }
    }
}