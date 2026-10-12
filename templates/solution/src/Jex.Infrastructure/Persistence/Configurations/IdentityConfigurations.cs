using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : EntityConfiguration<User>
{
    protected override void Configure(TableFluent<User> table)
    {
        table.Property(u => u.Id).IsPrimary(true).IsIdentity(true);
        table.Property(u => u.NormalizedEmail).StringLength(256);
        table.Property(u => u.NormalizedUserName).StringLength(256);
        table.Index("uk_users_normalized_email", nameof(User.NormalizedEmail), true);
        table.Index("uk_users_normalized_user_name", nameof(User.NormalizedUserName), true);
    }
}

public sealed class ApplicationRoleConfiguration : EntityConfiguration<ApplicationRole>
{
    protected override void Configure(TableFluent<ApplicationRole> table)
    {
        table.Property(r => r.Id).IsPrimary(true).IsIdentity(true);
        table.Property(r => r.NormalizedName).StringLength(256);
        table.Index("uk_roles_normalized_name", nameof(ApplicationRole.NormalizedName), true);
    }
}

public sealed class UserRoleConfiguration : EntityConfiguration<UserRole>
{
    protected override void Configure(TableFluent<UserRole> table)
    {
        table.Property(ur => ur.UserId).IsPrimary(true);
        table.Property(ur => ur.RoleId).IsPrimary(true);
    }
}
