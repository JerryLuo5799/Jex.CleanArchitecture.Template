using Jex.Domain.Entities;
using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Public user DTO. Password hash and internal Identity fields (stamps, lockout) are never exposed.
/// Mapping is hand-written (see ADR-0009): keep <see cref="From"/> next to the DTO.
/// </summary>
public sealed record UserDto(
    long Id,
    string FirstName,
    string LastName,
    string? Email,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public string FullName => $"{FirstName} {LastName}";

    public static UserDto From(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Status, user.CreatedAt, user.UpdatedAt);
}
