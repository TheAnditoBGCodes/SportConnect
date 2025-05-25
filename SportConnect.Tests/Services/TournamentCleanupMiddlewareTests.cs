using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SportConnect.DataAccess;
using SportConnect.Models;
using SportConnect.Services;

namespace SportConnect.Tests.Services
{
    [TestFixture]
    public class TournamentCleanupMiddlewareTests
    {
        private RequestDelegate _next;
        private ILogger<TournamentCleanupMiddleware> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private SportConnectDbContext _dbContext;
        private TournamentCleanupMiddleware _middleware;

        [SetUp]
        public void Setup()
        {
            _next = context => Task.CompletedTask;
            var loggerMock = new Mock<ILogger<TournamentCleanupMiddleware>>();
            _logger = loggerMock.Object;

            var options = new DbContextOptionsBuilder<SportConnectDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SportConnectDbContext(options);

            var scopeMock = new Mock<IServiceScope>();
            var providerMock = new Mock<IServiceProvider>();
            providerMock.Setup(p => p.GetService(typeof(SportConnectDbContext))).Returns(_dbContext);
            scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
            _serviceScopeFactory = scopeFactoryMock.Object;

            _middleware = new TournamentCleanupMiddleware(_next, _logger, _serviceScopeFactory);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDependenciesSuccessfully()
        {
            var next = new RequestDelegate(context => Task.CompletedTask);
            var loggerMock = new Mock<ILogger<TournamentCleanupMiddleware>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var middleware = new TournamentCleanupMiddleware(next, loggerMock.Object, scopeFactoryMock.Object);

            Assert.IsNotNull(middleware);
        }
        private Tournament CreateTournament(string id, string name, DateTime date, DateTime? deadline = null)
        {
            return new Tournament
            {
                Id = id,
                Name = name,
                Date = date.ToString(),
                Deadline = deadline?.ToString() ?? date.AddDays(-1).ToString(), // default: 1 day before tournament date
                Country = "Bulgaria",
                Description = "Test tournament",
                ImageUrl = "https://example.com/image.jpg",
                OrganizerId = $"org-{id}",
                SportId = "1"
            };
        }
        [Test]
        public async Task InvokeAsync_NoExpiredTournaments_DoesNotRemoveAnything()
        {
            _dbContext.Tournaments.Add(CreateTournament(
                "1", "Future", DateTime.Now.AddDays(5), DateTime.Now.AddDays(3)));
            await _dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            await _middleware.InvokeAsync(context);

            Assert.AreEqual(1, _dbContext.Tournaments.Count());
            Assert.AreEqual(0, _dbContext.Participations.Count());
        }

        [Test]
        public async Task InvokeAsync_WithExpiredTournaments_RemovesTournamentsAndParticipations()
        {
            var expired1 = CreateTournament("1", "Old 1", DateTime.Now.AddDays(-5), DateTime.Now.AddDays(-10));
            var expired2 = CreateTournament("2", "Old 2", DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-3));
            var future = CreateTournament("3", "Future", DateTime.Now.AddDays(5), DateTime.Now.AddDays(3));

            _dbContext.Tournaments.AddRange(expired1, expired2, future);
            _dbContext.Participations.AddRange(
                new Participation { TournamentId = "1", ParticipantId = "u1" },
                new Participation { TournamentId = "2", ParticipantId = "u2" },
                new Participation { TournamentId = "3", ParticipantId = "u3" });
            await _dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            await _middleware.InvokeAsync(context);

            Assert.AreEqual(1, _dbContext.Tournaments.Count());
            Assert.AreEqual(1, _dbContext.Participations.Count());
            Assert.IsTrue(_dbContext.Tournaments.Any(t => t.Id == "3"));
        }

        [Test]
        public async Task InvokeAsync_WithExpiredTournamentsButNoParticipations_OnlyRemovesTournaments()
        {
            var expired = CreateTournament("1", "Old", DateTime.Now.AddDays(-5), DateTime.Now.AddDays(-7));
            _dbContext.Tournaments.Add(expired);
            await _dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            await _middleware.InvokeAsync(context);

            Assert.AreEqual(0, _dbContext.Tournaments.Count());
        }

        [Test]
        public void InvokeAsync_DbContextThrowsException_PropagatesException()
        {
            var mockContext = new Mock<SportConnectDbContext>(new DbContextOptionsBuilder<SportConnectDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            mockContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Test error"));

            var providerMock = new Mock<IServiceProvider>();
            providerMock.Setup(p => p.GetService(typeof(SportConnectDbContext))).Returns(mockContext.Object);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);

            var factoryMock = new Mock<IServiceScopeFactory>();
            factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            var middleware = new TournamentCleanupMiddleware(_next, _logger, factoryMock.Object);
            var context = new DefaultHttpContext();

            Assert.ThrowsAsync<NullReferenceException>(async () => await middleware.InvokeAsync(context));
        }
        [Test]
        public async Task InvokeAsync_AlwaysCallsNextMiddleware_EvenAfterCleanup()
        {
            var expired = CreateTournament(
                "1",
                "Old",
                DateTime.Now.AddDays(-5),
                DateTime.Now.AddDays(-7) // Deadline before the tournament date
            );
            _dbContext.Tournaments.Add(expired);
            await _dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            await _middleware.InvokeAsync(context);

            Assert.Pass("Middleware executed successfully and continued");
        }
    }
}
