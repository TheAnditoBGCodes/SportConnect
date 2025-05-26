using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;
using System.Linq.Expressions;

namespace SportConnect.Services.Tournament
{
    public class TournamentService : ITournamentService
    {
        private readonly IRepository<SportConnect.Models.Tournament> repository;

        public TournamentService(IRepository<Models.Tournament> repository)
        {
            this.repository = repository;
        }

        public Task Add(Models.Tournament entity)
        {
            return repository.Add(entity);
        }

        public Task<IEnumerable<Models.Tournament>> AllWithIncludes(params Expression<Func<Models.Tournament, object>>[] includes)
        {
            return repository.AllWithIncludes(includes);
        }

        public async Task<IEnumerable<Models.Tournament>> AllParticipatedTournaments(string currentUser)
        {
            var tournaments = await repository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport);
            return tournaments.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser));
        }

        public async Task<IEnumerable<Models.Tournament>> AllOtherParticipatedTournaments(string currentUserId)
        {
            var tournaments = await repository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport);
            return tournaments.Where(t => t.Participations.Any(p => p.ParticipantId == currentUserId));
        }

        public Task Delete(Models.Tournament entity)
        {
            return repository.Delete(entity);
        }

        public Task DeleteRange(IEnumerable<Models.Tournament> range)
        {
            return repository.DeleteRange(range);
        }

        public Task<IEnumerable<Models.Tournament>> GetAll()
        {
            return repository.GetAll();
        }

        public Task<IEnumerable<Models.Tournament>> GetAllBy(Expression<Func<Models.Tournament, bool>> filter)
        {
            return repository.GetAllBy(filter);
        }

        public Task<Models.Tournament?> GetById(string id)
        {
            return repository.GetById(id);
        }

        public Task<bool> IsPropertyUnique(Expression<Func<Models.Tournament, bool>> predicate)
        {
            return repository.IsPropertyUnique(predicate);
        }

        public Task Save()
        {
            return repository.Save();
        }

        public Task Update(Models.Tournament entity)
        {
            return repository.Update(entity);
        }
        public async Task<IEnumerable<Models.Tournament>> TournamentsOfSport(string id)
        {
            var tournaments = await repository.AllWithIncludes(x => x.Organizer, x => x.Sport, x => x.Participations);
            return tournaments.Where(x => x.SportId == id);
        }

        public async Task<IEnumerable<Models.Tournament>> TournamentsOfUser(string id)
        {
            var tournaments = await repository.AllWithIncludes(x => x.Organizer, x => x.Sport, x => x.Participations);
            return tournaments.Where(x => x.OrganizerId == id);
        }
    }
}