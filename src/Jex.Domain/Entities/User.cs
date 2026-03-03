using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jex.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Jex.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// Extends <see cref="IdentityUser{TKey}"/> so that ASP.NET Core Identity can manage
/// authentication and authorization, while adding the domain-specific fields from the
/// original User entity. Both "tables" are thus merged into a single "users" table.
/// ORM table and column mappings are configured via data annotation attributes.
/// </summary>
[Table("users")]
public class User : IdentityUser<long>, IAuditableEntity
{
    [MaxLength(100)]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
