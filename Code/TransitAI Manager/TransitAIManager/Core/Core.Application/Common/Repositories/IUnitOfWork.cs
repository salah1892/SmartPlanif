namespace Core.Application.Common.Repositories
{
    // IAsyncDisposable est recommandé pour s'assurer que la transaction est toujours libérée
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task SaveAsync(CancellationToken cancellationToken = default);

        void Save();

        //Nouvelles méthodes pour la transaction
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}