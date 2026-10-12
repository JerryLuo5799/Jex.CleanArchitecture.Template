namespace Jex.Application.Common.Interfaces;

/// <summary>
/// Transaction boundary abstraction (implemented over FreeSql's UnitOfWorkManager).
/// Only the <see cref="Behaviours.UnitOfWorkBehaviour{TRequest, TResponse}"/> calls this;
/// handlers and repositories never begin or commit transactions themselves.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>True when an ambient transaction is already open for the current scope.</summary>
    bool IsActive { get; }

    /// <summary>Starts a transaction, or joins the ambient one. Dispose without commit rolls back.</summary>
    IUnitOfWorkScope Begin();
}

/// <summary>A started (or joined) transaction. Only the owner's <see cref="CommitAsync"/> commits.</summary>
public interface IUnitOfWorkScope : IDisposable
{
    /// <summary>True when this scope created the transaction and is responsible for committing it.</summary>
    bool IsOwner { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);
}
