using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;
using System.Linq.Expressions;
using SportConnect.Services.Sport;

namespace SportConnect.Services.User
{
    public class UserService : IUserService
    {
        private readonly IRepository<SportConnect.Models.SportConnectUser> repository;

        public UserService(IRepository<Models.SportConnectUser> repository)
        {
            this.repository = repository;
        }

        public Task Add(Models.SportConnectUser entity)
        {
            return repository.Add(entity);
        }

        public Task<IEnumerable<Models.SportConnectUser>> AllWithIncludes(params Expression<Func<Models.SportConnectUser, object>>[] includes)
        {
            return repository.AllWithIncludes(includes);
        }

        public Task Delete(Models.SportConnectUser entity)
        {
            return repository.Delete(entity);
        }

        public Task DeleteRange(IEnumerable<Models.SportConnectUser> range)
        {
            return repository.DeleteRange(range);
        }

        public Task<IEnumerable<Models.SportConnectUser>> GetAll()
        {
            return repository.GetAll();
        }

        public Task<IEnumerable<Models.SportConnectUser>> GetAllBy(Expression<Func<Models.SportConnectUser, bool>> filter)
        {
            return repository.GetAllBy(filter);
        }

        public async Task<IEnumerable<Models.SportConnectUser>> AllParticipants(string id)
        {
            var tournaments = await repository.AllWithIncludes(t => t.Participations);
            return tournaments.Where(t => t.Participations.Any(p => p.TournamentId.ToString() == id.ToString()));
        }

        public Task<Models.SportConnectUser?> GetById(string id)
        {
            return repository.GetById(id);
        }

        public Task<bool> IsPropertyUnique(Expression<Func<Models.SportConnectUser, bool>> predicate)
        {
            return repository.IsPropertyUnique(predicate);
        }

        public Task Save()
        {
            return repository.Save();
        }

        public Task Update(Models.SportConnectUser entity)
        {
            return repository.Update(entity);
        }
    }
}