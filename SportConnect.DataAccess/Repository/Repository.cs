using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using System.Linq.Expressions;

namespace SportConnect.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly SportConnectDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(SportConnectDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task Add(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> AllWithIncludes(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var item in includes)
            {
                query = query.Include(item);
            }
            return await query.ToListAsync();
        }

        public async Task<bool> IsPropertyUnique(Expression<Func<T, bool>> predicate)
        {
            return !await _dbSet.AnyAsync(predicate);
        }

        public async Task Delete(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRange(IEnumerable<T> range)
        {
            _dbSet.RemoveRange(range);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllBy(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.Where(filter).ToListAsync();
        }

        public async Task<T?> GetById(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T?> GetUserById(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task Update(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}