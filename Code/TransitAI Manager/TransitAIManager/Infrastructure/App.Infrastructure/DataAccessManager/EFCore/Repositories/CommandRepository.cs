using System.Linq.Expressions;
using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.Extensions;
using Core.Application.Common.Repositories;
using Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.DataAccessManager.EFCore.Repositories
{
    public class CommandRepository<T> : ICommandRepository<T> where T : BaseEntity
    {
        protected readonly CommandContext _context;
        private readonly DbSet<T> _dbSet;

        public CommandRepository(CommandContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.CreatedAtUtc = DateTime.UtcNow;
            await _context.AddAsync(entity, cancellationToken);
        }

        public void Create(T entity)
        {
            entity.CreatedAtUtc = DateTime.UtcNow;
            _context.Add(entity);
        }

        public void Update(T entity)
        {
            entity.UpdatedAtUtc = DateTime.UtcNow;
            _context.Update(entity);
        }

        public void Delete(T entity)
        {
            entity.IsDeleted = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            _context.Update(entity);
        }

        public void Purge(T entity)
        {
            _context.Remove(entity);
        }

        public virtual async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Set<T>()
                .IsDeletedEqualTo()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            return entity;
        }

        public virtual T? Get(string id)
        {
            var entity = _context.Set<T>()
                .IsDeletedEqualTo()
                .SingleOrDefault(x => x.Id == id);

            return entity;
        }

        public virtual IQueryable<T> GetQuery()
        {
            var query = _context.Set<T>().AsQueryable();

            return query;
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query.ToList();
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            var query = await _context.Set<T>().AnyAsync(predicate, cancellationToken);
            return query;
        }

        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (include != null)
            {
                query = include(query);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}