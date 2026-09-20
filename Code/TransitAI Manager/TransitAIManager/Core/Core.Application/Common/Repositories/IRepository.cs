using System.Linq.Expressions;

namespace Core.Application.Common.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task CreateAsync(T entity, CancellationToken cancellationToken = default);
        void Create(T entity);

        void Update(T entity);

        void Delete(T entity);

        void Purge(T entity);

        Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
        T? Get(string id);

        IQueryable<T> GetQuery();

        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? predicate = null);  
    }
}