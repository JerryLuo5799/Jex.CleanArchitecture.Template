namespace Jex.Infrastructure.Persistence.Configurations;

/// <summary>
/// FreeSql Code First configuration for one entity (keys, lengths, indexes, navigations).
/// Implementations are discovered by reflection: adding a class is all it takes to register an entity
/// for schema sync, migrations and tests (see <see cref="EntityConfigurations"/>).
/// </summary>
public interface IEntityConfiguration
{
    Type EntityType { get; }

    void Configure(ICodeFirst codeFirst);
}

/// <summary>Typed helper so configurations get the fluent <see cref="TableFluent{T}"/> API.</summary>
public abstract class EntityConfiguration<TEntity> : IEntityConfiguration
    where TEntity : class
{
    public Type EntityType => typeof(TEntity);

    public void Configure(ICodeFirst codeFirst) => codeFirst.ConfigEntity<TEntity>(Configure);

    protected abstract void Configure(TableFluent<TEntity> table);
}
