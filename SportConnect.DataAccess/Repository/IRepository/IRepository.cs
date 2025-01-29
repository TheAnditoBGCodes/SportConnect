using System.Linq.Expressions;

namespace SportConnect.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        T GetById(int id);

        void Add(T entity);

        void Update(T entity);

        void Delete(T entity);

        IEnumerable<T> GetAll();

        IEnumerable<T> GetAllBy(Expression<Func<T,bool>> filter);

        IEnumerable<T> AllWithIncludes(params Expression<Func<T, object>>[] includes);
    }
}
