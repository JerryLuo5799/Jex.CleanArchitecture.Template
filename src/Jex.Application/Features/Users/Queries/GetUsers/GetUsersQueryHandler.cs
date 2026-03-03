using MediatR;
using Facet.Extensions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Handles <see cref="GetUsersQuery"/>.
/// Uses the Facet-generated compile-time extension for bulk mapping.
/// </summary>
public sealed class GetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

        // Facet generates a compile-time ToFacet<TSource, TFacet>() extension per entity.
        return users.Select(u => u.ToFacet<User, UserDto>()).ToList();
    }
}
