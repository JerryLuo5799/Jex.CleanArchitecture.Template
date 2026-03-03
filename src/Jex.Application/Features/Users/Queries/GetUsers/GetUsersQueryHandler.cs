using MediatR;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Queries.GetUserById;

namespace Jex.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Handles <see cref="GetUsersQuery"/>.
/// Maps each User → UserDto, excluding password and internal Identity fields.
/// </summary>
public sealed class GetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        return users.Select(GetUserByIdQueryHandler.ToDto).ToList();
    }
}
