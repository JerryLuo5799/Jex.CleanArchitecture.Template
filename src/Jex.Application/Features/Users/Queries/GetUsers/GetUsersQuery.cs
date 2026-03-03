using MediatR;
using Jex.Application.Features.Users.Queries.GetUserById;

namespace Jex.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to retrieve a paginated list of users.
/// </summary>
public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<List<UserDto>>;
