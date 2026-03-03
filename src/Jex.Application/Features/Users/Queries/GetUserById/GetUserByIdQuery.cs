using MediatR;
using Jex.Application.Features.Users.Queries.GetUserById;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Query to retrieve a single user by their primary key.
/// </summary>
public sealed record GetUserByIdQuery(long Id) : IRequest<UserDto>;
