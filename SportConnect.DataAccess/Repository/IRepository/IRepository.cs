using System.Linq.Expressions;

namespace SportConnect.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<bool> IsPropertyUnique(Expression<Func<T, bool>> predicate);
        Task DeleteRange(IEnumerable<T> range);

        Task<T> GetUserById(string id);
        
        Task<T> GetById(int id);

        Task Add(T entity);

        Task Save();

        Task Update(T entity);

        Task Delete(T entity);

        Task<IEnumerable<T>> GetAll();

        Task<IEnumerable<T>> GetAllBy(Expression<Func<T,bool>> filter);

        Task<IEnumerable<T>> AllWithIncludes(params Expression<Func<T, object>>[] includes);
    }
}
