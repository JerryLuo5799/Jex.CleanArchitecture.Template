using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Jex.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// Maps to the "user_roles" table.
/// The composite primary key (UserId + RoleId) is configured in the Infrastructure layer
/// via FreeSQL's fluent entity API.
/// </summary>
[Table("user_roles")]
public class UserRole : IdentityUserRole<long>
{
}
