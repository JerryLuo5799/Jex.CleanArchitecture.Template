using Jex.Application.Common.Interfaces;
using IUnitOfWork = Jex.Application.Common.Interfaces.IUnitOfWork;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> over FreeSql's <see cref="UnitOfWorkManager"/> (scoped). Repositories
/// created with the same manager join the transaction automatically.
/// </summary>
public sealed class FreeSqlUnitOfWork(UnitOfWorkManager manager) : IUnitOfWork
{
    public bool IsActive => manager.Current is not null;

    public IUnitOfWorkScope Begin()
    {
        var isOwner = manager.Current is null;
        var unitOfWork = manager.Begin(Propagation.Required);
        return new Scope(unitOfWork, isOwner);
    }

    private sealed class Scope(FreeSql.IUnitOfWork unitOfWork, bool isOwner) : IUnitOfWorkScope
    {
        private bool _completed;

        public bool IsOwner { get; } = isOwner;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            unitOfWork.Commit();
            _completed = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_completed)
            {
                // Dispose without Commit: FreeSql rolls back the (owner's) transaction.
                unitOfWork.Rollback();
            }

            unitOfWork.Dispose();
        }
    }
}
