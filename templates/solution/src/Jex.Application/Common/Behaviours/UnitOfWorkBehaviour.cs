using System.Reflection;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;

namespace Jex.Application.Common.Behaviours;

/// <summary>
/// Wraps every <see cref="ICommand"/> / <see cref="ICommand{TResponse}"/> in a transaction (innermost
/// behaviour, so it only surrounds the handler). The owner of the transaction dispatches domain
/// events after the handler returns, then commits; any exception rolls back and discards the events.
/// Nested commands sent from a handler join the ambient transaction and never commit on their own.
/// Commands marked <see cref="NonTransactionalAttribute"/> and all queries are passed through.
/// </summary>
public sealed class UnitOfWorkBehaviour<TRequest, TResponse>(IUnitOfWork unitOfWork, IDomainEventDispatcher domainEvents)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool s_isTransactional =
        typeof(TRequest).IsAssignableTo(typeof(IBaseRequest))
        && typeof(TRequest).GetInterfaces().Any(i => i == typeof(ICommand) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)))
        && typeof(TRequest).GetCustomAttribute<NonTransactionalAttribute>() is null;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!s_isTransactional)
        {
            return await next(cancellationToken);
        }

        using var scope = unitOfWork.Begin();

        try
        {
            var response = await next(cancellationToken);

            if (scope.IsOwner)
            {
                await domainEvents.DispatchAsync(cancellationToken);
                await scope.CommitAsync(cancellationToken);
            }

            return response;
        }
        catch
        {
            if (scope.IsOwner)
            {
                domainEvents.Discard();
            }

            throw;
        }
    }
}
