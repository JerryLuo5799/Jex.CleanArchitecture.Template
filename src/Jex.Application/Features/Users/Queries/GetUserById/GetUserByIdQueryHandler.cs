using MediatR;
using Jex.Application.Common;
using Jex.Application.Common.Exceptions;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Handles <see cref="GetUserByIdQuery"/>.
/// Maps User → UserDto, excluding password and internal Identity fields.
/// </summary>
public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        return ToDto(user);
    }

    internal static UserDto ToDto(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Status, user.CreatedAt, user.UpdatedAt);
}
