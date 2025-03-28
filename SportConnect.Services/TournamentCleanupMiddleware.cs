using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SportConnect.DataAccess;

namespace SportConnect.Services
{
    public class TournamentCleanupMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TournamentCleanupMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TournamentCleanupMiddleware(RequestDelegate next, ILogger<TournamentCleanupMiddleware> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (var scope = _serviceScopeFactory.CreateScope()) // Create a scope for the DbContext
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SportConnectDbContext>(); // Get the DbContext from the scope

                // Perform the cleanup logic for expired tournaments
                var expiredTournaments = await dbContext.Tournaments
                    .Where(t => t.Date <= DateTime.Now)
                    .ToListAsync();

                if (expiredTournaments.Any())
                {
                    // First, remove all participations related to the expired tournaments
                    var tournamentIds = expiredTournaments.Select(t => t.Id).ToList();
                    var relatedParticipations = dbContext.Participations
                        .Where(p => tournamentIds.Contains(p.TournamentId));

                    dbContext.Participations.RemoveRange(relatedParticipations);

                    // Now remove the expired tournaments
                    dbContext.Tournaments.RemoveRange(expiredTournaments);

                    // Save changes to the database
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"{expiredTournaments.Count} expired tournaments and their participations cleaned up.");
                }
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}