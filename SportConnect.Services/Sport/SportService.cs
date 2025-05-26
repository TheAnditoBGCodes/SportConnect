using SportConnect.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Models;
using System.Linq.Expressions;
using SportConnect.Services.Sport;

namespace SportConnect.Services.Sport
{
    public class SportService : ISportService
    {
        private readonly IRepository<SportConnect.Models.Sport> repository;

        public SportService(IRepository<Models.Sport> repository)
        {
            this.repository = repository;
        }

        public Task Add(Models.Sport entity)
        {
            return repository.Add(entity);
        }

        public Task<IEnumerable<Models.Sport>> AllWithIncludes(params Expression<Func<Models.Sport, object>>[] includes)
        {
            return repository.AllWithIncludes(includes);
        }

        public Task Delete(Models.Sport entity)
        {
            return repository.Delete(entity);
        }

        public Task DeleteRange(IEnumerable<Models.Sport> range)
        {
            return repository.DeleteRange(range);
        }

        public Task<IEnumerable<Models.Sport>> GetAll()
        {
            return repository.GetAll();
        }

        public Task<IEnumerable<Models.Sport>> GetAllBy(Expression<Func<Models.Sport, bool>> filter)
        {
            return repository.GetAllBy(filter);
        }

        public Task<Models.Sport?> GetById(string id)
        {
            return repository.GetById(id);
        }

        public Task<bool> IsPropertyUnique(Expression<Func<Models.Sport, bool>> predicate)
        {
            return repository.IsPropertyUnique(predicate);
        }

        public Task Save()
        {
            return repository.Save();
        }

        public Task Update(Models.Sport entity)
        {
            return repository.Update(entity);
        }
    }
}