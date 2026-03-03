using FreeSql;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Configures FreeSQL entity mappings using Fluent API.
/// Keeps all ORM concerns out of the Domain layer.
/// </summary>
internal static class FreeSqlEntityConfiguration
{
    internal static void Configure(IFreeSql freeSql)
    {
        freeSql.CodeFirst.ConfigEntity<User>(eb =>
        {
            eb.Name("users");
            eb.Property(u => u.Id).IsPrimary(true).IsIdentity(true);
            eb.Property(u => u.FirstName).StringLength(100).IsNullable(false);
            eb.Property(u => u.LastName).StringLength(100).IsNullable(false);
            eb.Property(u => u.Email).StringLength(200).IsNullable(false);
            eb.Property(u => u.PasswordHash).StringLength(256).IsNullable(false);
        });
    }
}
