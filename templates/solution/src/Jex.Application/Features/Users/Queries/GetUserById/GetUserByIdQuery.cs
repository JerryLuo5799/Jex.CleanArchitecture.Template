namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>Retrieves a single user by primary key.</summary>
public sealed record GetUserByIdQuery(long Id) : IRequest<UserDto>;
