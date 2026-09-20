using System.Linq.Expressions;
using Core.Application.Common.Repositories;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.DataAccessManager.EFCore.Repositories
{
    public class Repository<T>: IRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public void Create(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }

        public void Purge(T entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public T? Get(string id)
        {
            return _dbSet.Find(id);
        }

        public IQueryable<T> GetQuery()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            return predicate == null
                ? await _dbSet.ToListAsync(cancellationToken)
                : await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null
                ? _dbSet.ToList()
                : _dbSet.Where(predicate).ToList();
        }
    }
}