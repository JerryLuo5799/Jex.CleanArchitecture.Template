using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jex.Domain.Enums;

namespace Jex.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// ORM table and column mappings are configured via data annotation attributes.
/// </summary>
[Table("users")]
public class User : BaseEntity
{
    [MaxLength(100)]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Required]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
