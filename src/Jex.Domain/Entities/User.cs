using Jex.Domain.Enums;

namespace Jex.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// Code First: FreeSQL table/column configuration lives in Infrastructure (Fluent API),
/// keeping this Domain entity free of infrastructure concerns.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public string FullName => $"{FirstName} {LastName}";
}
