using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.Infrastructure.DataAccessManager.EFCore.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CommandContext _context;
        private IDbContextTransaction? _currentTransaction; // Pour garder une référence à la transaction en cours

        public UnitOfWork(CommandContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            // On ne commence une nouvelle transaction que s'il n'y en a pas déjà une
            if (_currentTransaction is null)
            {
                _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _currentTransaction?.CommitAsync(cancellationToken)!;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction is not null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _currentTransaction?.RollbackAsync(cancellationToken)!;
            }
            finally
            {
                if (_currentTransaction is not null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }
    // Implémentation de IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
            }
            await _context.DisposeAsync();
        }

    }
}