using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jex.Domain.Enums;

namespace Jex.Domain.Entities;

/// <summary>
/// Application user. Extends <see cref="IdentityUser{TKey}"/> so ASP.NET Core Identity manages
/// credentials and lockout, while domain fields live on the same "users" table (ADR-0007).
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

    /// <summary>
    /// Persisted lockout end (UTC). <see cref="IdentityUser{TKey}.LockoutEnd"/> is a
    /// <see cref="DateTimeOffset"/>, which not every FreeSql provider maps (SQLite does not),
    /// so the stored column is a plain UTC <see cref="DateTime"/>.
    /// </summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public override DateTimeOffset? LockoutEnd
    {
        get => LockoutEndUtc is null ? null : new DateTimeOffset(DateTime.SpecifyKind(LockoutEndUtc.Value, DateTimeKind.Utc));
        set => LockoutEndUtc = value?.UtcDateTime;
    }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
