using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Jex.Domain.Entities;

/// <summary>
/// Represents an application role backed by ASP.NET Core Identity.
/// Maps to the "roles" table in the database.
/// </summary>
[Table("roles")]
public class ApplicationRole : IdentityRole<long>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
