using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// The entity types that make up the database schema. Used for startup schema sync (development),
/// migration script generation and tests. Add every new aggregate root / entity here.
/// </summary>
public static class EntityRegistry
{
    public static IReadOnlyList<Type> Types { get; } =
    [
        typeof(User),
        typeof(ApplicationRole),
        typeof(UserRole),
    ];
}
