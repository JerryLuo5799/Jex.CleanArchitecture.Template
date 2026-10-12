namespace Jex.Application.Common.Messaging;

/// <summary>
/// A read-only request. Queries run outside any transaction and must not change state.
/// Name: <c>Get{Entity}ByIdQuery</c>, <c>Get{Entities}Query</c>, <c>Get{Entities}WithPaginationQuery</c>.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
