namespace Jex.Application.Common.Messaging;

/// <summary>
/// A request that changes state. Commands run inside a unit of work: the
/// <see cref="Behaviours.UnitOfWorkBehaviour{TRequest, TResponse}"/> opens a transaction, dispatches
/// domain events after the handler succeeds and commits. Handlers never commit themselves.
/// Name: <c>{Verb}{Entity}Command</c>, e.g. <c>CreateOrderCommand</c>.
/// </summary>
public interface ICommand : IRequest;

/// <inheritdoc cref="ICommand"/>
public interface ICommand<out TResponse> : IRequest<TResponse>;
