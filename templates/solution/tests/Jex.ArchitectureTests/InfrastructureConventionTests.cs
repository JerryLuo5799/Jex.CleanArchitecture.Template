using System.Reflection;
using Jex.Application.Common.Interfaces;
using Jex.Infrastructure.Persistence.Configurations;
using Jex.Infrastructure.Persistence.Repositories;

namespace Jex.ArchitectureTests;

public class InfrastructureConventionTests
{
    private static readonly Assembly s_infrastructure = typeof(Jex.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Repositories_DeriveFromRepositoryBase_AndImplementAnApplicationInterface()
    {
        var repositories = s_infrastructure.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToList();

        var notDerived = repositories.Where(t => !IsSubclassOfRawGeneric(typeof(Repository<>), t)).ToList();
        notDerived.ShouldBeEmpty(
            $"Repositories derive from Repository<TEntity> so writes join the unit of work and get audit stamps: {Names(notDerived)}.");

        var noContract = repositories
            .Where(t => !t.GetInterfaces().Any(i => !i.IsGenericType && i.Namespace == typeof(IUserRepository).Namespace))
            .ToList();
        noContract.ShouldBeEmpty(
            $"Each repository implements an I{{Entity}}Repository interface from Jex.Application/Common/Interfaces so it is registered by convention: {Names(noContract)}.");
    }

    [Fact]
    public void EveryEntityWithATable_HasAnEntityConfiguration()
    {
        var entityTypes = typeof(Jex.Domain.Entities.User).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>() is not null)
            .ToList();

        var configured = EntityConfigurations.EntityTypes.ToHashSet();
        var missing = entityTypes.Where(t => !configured.Contains(t)).ToList();

        missing.ShouldBeEmpty(
            $"Add a {{Entity}}Configuration : EntityConfiguration<{{Entity}}> under Jex.Infrastructure/Persistence/Configurations for: {Names(missing)}. " +
            "That is what registers the entity for schema sync and migrations.");
    }

    [Fact]
    public void IdentityService_DoesNotTouchFreeSqlDirectly()
    {
        var identityService = s_infrastructure.GetType("Jex.Infrastructure.Identity.IdentityService")!;

        var parameters = identityService.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType.Namespace ?? string.Empty);

        parameters.ShouldNotContain(n => n.StartsWith("FreeSql", StringComparison.Ordinal),
            "IdentityService works through UserManager/repositories; direct IFreeSql access would bypass the unit of work.");
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type? type)
    {
        while (type is not null && type != typeof(object))
        {
            var current = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (current == generic)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    private static string Names(IEnumerable<Type> types) => string.Join(", ", types.Select(t => t.Name));
}
