using MediatR;
using Facet.Extensions;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Handles <see cref="GetUserByIdQuery"/>.
/// Uses the Facet-generated projection to map User → UserDto efficiently.
/// </summary>
public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        // Facet generates ToFacet<TSource, TFacet>() as a compile-time extension method.
        return user.ToFacet<User, UserDto>();
    }
}
