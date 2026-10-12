namespace Jex.Infrastructure.Persistence.Configurations;

/// <summary>Reflection-based registry of every <see cref="IEntityConfiguration"/> in this assembly.</summary>
public static class EntityConfigurations
{
    private static readonly Lazy<IReadOnlyList<IEntityConfiguration>> s_all = new(() =>
        typeof(EntityConfigurations).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEntityConfiguration).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => (IEntityConfiguration)Activator.CreateInstance(t)!)
            .ToList());

    public static IReadOnlyList<IEntityConfiguration> All => s_all.Value;

    /// <summary>Every entity type that makes up the schema (schema sync, migrations, tests).</summary>
    public static IReadOnlyList<Type> EntityTypes => All.Select(c => c.EntityType).ToList();

    public static void ApplyAll(ICodeFirst codeFirst)
    {
        foreach (var configuration in All)
        {
            configuration.Configure(codeFirst);
        }
    }
}
