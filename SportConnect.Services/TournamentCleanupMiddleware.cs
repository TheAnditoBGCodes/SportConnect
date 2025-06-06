﻿using Microsoft.AspNetCore.Http;
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
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SportConnectDbContext>();

                var expiredTournaments = await dbContext.Tournaments.ToListAsync();

                var expired = expiredTournaments.Where(t => DateTime.TryParse(t.Date, out var date) && date <= DateTime.Now).ToList();

                if (expired.Any())
                {
                    var tournamentIds = expired.Select(t => t.Id).ToList();
                    var relatedParticipations = dbContext.Participations
                        .Where(p => tournamentIds.Contains(p.TournamentId));

                    dbContext.Participations.RemoveRange(relatedParticipations);

                    dbContext.Tournaments.RemoveRange(expired);

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"{expired.Count} expired tournaments and their participations cleaned up.");
                }
            }

            await _next(context);
        }
    }
}