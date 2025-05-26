using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;
using System.Linq.Expressions;
using SportConnect.Services.Sport;

namespace SportConnect.Services.Participation
{
    public class ParticipationService : IParticipationService
    {
        private readonly IRepository<SportConnect.Models.Participation> repository;

        public ParticipationService(IRepository<Models.Participation> repository)
        {
            this.repository = repository;
        }

        public Task Add(Models.Participation entity)
        {
            return repository.Add(entity);
        }

        public Task<IEnumerable<Models.Participation>> AllWithIncludes(params Expression<Func<Models.Participation, object>>[] includes)
        {
            return repository.AllWithIncludes(includes);
        }

        public Task Delete(Models.Participation entity)
        {
            return repository.Delete(entity);
        }

        public Task DeleteRange(IEnumerable<Models.Participation> range)
        {
            return repository.DeleteRange(range);
        }

        public Task<IEnumerable<Models.Participation>> GetAll()
        {
            return repository.GetAll();
        }

        public Task<IEnumerable<Models.Participation>> GetAllBy(Expression<Func<Models.Participation, bool>> filter)
        {
            return repository.GetAllBy(filter);
        }

        public Task<Models.Participation?> GetById(string id)
        {
            return repository.GetById(id);
        }

        public async Task<Models.Participation> GetParticipation(string userId, string tournamentId)
        {
            var participations = await repository.GetAll();
            return participations.FirstOrDefault(x => x.ParticipantId == userId && x.TournamentId.ToString() == tournamentId.ToString());
        }

        public Task<bool> IsPropertyUnique(Expression<Func<Models.Participation, bool>> predicate)
        {
            return repository.IsPropertyUnique(predicate);
        }

        public Task Save()
        {
            return repository.Save();
        }

        public Task Update(Models.Participation entity)
        {
            return repository.Update(entity);
        }
    }
}