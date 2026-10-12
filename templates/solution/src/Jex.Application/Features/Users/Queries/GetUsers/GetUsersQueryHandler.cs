using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Models;
using Jex.Application.Features.Users.Queries.GetUserById;

namespace Jex.Application.Features.Users.Queries.GetUsers;

/// <summary>Handles <see cref="GetUsersQuery"/>.</summary>
public sealed class GetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, PagedData<UserDto>>
{
    public async Task<PagedData<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = await userRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken: cancellationToken);
        return page.Map(UserDto.From);
    }
}
