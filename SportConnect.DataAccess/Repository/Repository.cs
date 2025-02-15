using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using System.Linq.Expressions;

namespace SportConnect.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private SportConnectDbContext _context;
        private DbSet<T> _dbSet;

        public Repository(SportConnectDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public IEnumerable<T> AllWithIncludes(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var item in includes)
            {
                query = query.Include(item);
            }
            return query;
        }
        public bool IsPropertyUnique(Expression<Func<T, bool>> predicate)
        {
            return !_dbSet.Any(predicate);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }

        public void DeleteRange(IEnumerable<T> range)
        {
            _dbSet.RemoveRange(range);
            _context.SaveChanges();
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet;
        }

        public IEnumerable<T> GetAllBy(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = _dbSet.Where(filter);
            return query;
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public T GetUserById(string id)
        {
            return _dbSet.Find(id);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }
    }
}
