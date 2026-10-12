using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>Handles <see cref="GetUserByIdQuery"/>.</summary>
public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        return UserDto.From(user);
    }
}
