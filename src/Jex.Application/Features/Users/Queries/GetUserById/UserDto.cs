using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Public user DTO exposing only domain-relevant fields.
/// Password and internal Identity fields (SecurityStamp, ConcurrencyStamp, etc.) are excluded.
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
}
