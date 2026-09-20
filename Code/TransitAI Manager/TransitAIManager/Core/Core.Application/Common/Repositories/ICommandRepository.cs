using System.Linq.Expressions;
using Core.Domain.Common;

namespace Core.Application.Common.Repositories
{
    public interface ICommandRepository<T> where T : BaseEntity
    {
        Task CreateAsync(T entity, CancellationToken cancellationToken = default);

        void Create(T entity);

        void Update(T entity);

        void Delete(T entity);

        void Purge(T entity);

        Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);

        T? Get(string id);

        IQueryable<T> GetQuery();
        // Nouvelle méthode pour récupérer toutes les entités avec un prédicat asynchrone
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        // Nouvelle méthode pour récupérer toutes les entités avec un prédicat synchrone
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? predicate = null);
        // ✅ Nouvelle méthode : existe-t-il un élément ?
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync(Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null, CancellationToken cancellationToken = default);
    }
}