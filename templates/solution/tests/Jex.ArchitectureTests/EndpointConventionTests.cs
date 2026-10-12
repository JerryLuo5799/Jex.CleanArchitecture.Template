using System.Reflection;
using Jex.WebAPI.Infrastructure;

namespace Jex.ArchitectureTests;

/// <summary>
/// Endpoint conventions (AGENTS.md §3/§5): every HTTP surface is an <see cref="IEndpointGroup"/> in
/// <c>Jex.WebAPI.Endpoints</c>, groups declare an explicit route prefix, there are no MVC controllers,
/// and endpoint names are unique across the API.
/// </summary>
public class EndpointConventionTests
{
    private static readonly Assembly s_webApiAssembly = typeof(IEndpointGroup).Assembly;

    private static IEnumerable<Type> EndpointGroups =>
        s_webApiAssembly.GetExportedTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpointGroup).IsAssignableFrom(t));

    [Fact]
    public void EndpointGroups_LiveInEndpointsNamespace_AndAreSealed()
    {
        foreach (var group in EndpointGroups)
        {
            group.Namespace.ShouldBe("Jex.WebAPI.Endpoints",
                $"{group.Name} implements IEndpointGroup but is not in Jex.WebAPI/Endpoints/. Move it there (see AGENTS.md §3).");
            group.IsSealed.ShouldBeTrue($"{group.Name} must be sealed.");
        }
    }

    [Fact]
    public void EndpointGroups_DeclareRoutePrefixUnderApi()
    {
        foreach (var group in EndpointGroups)
        {
            var prefix = (string?)group.GetProperty(nameof(IEndpointGroup.RoutePrefix), BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

            prefix.ShouldNotBeNullOrWhiteSpace($"{group.Name}.RoutePrefix must be set, e.g. \"/orders\".");
            prefix.ShouldStartWith("/", Case.Sensitive, $"{group.Name}.RoutePrefix must start with '/' (it is mapped under /api).");
            prefix.ShouldNotStartWith("/api", Case.Insensitive, $"{group.Name}.RoutePrefix must not repeat the /api prefix.");
        }
    }

    [Fact]
    public void RoutePrefixes_AreUnique()
    {
        var prefixes = EndpointGroups
            .Select(g => (Group: g.Name, Prefix: (string?)g.GetProperty(nameof(IEndpointGroup.RoutePrefix))?.GetValue(null)))
            .ToList();

        var duplicates = prefixes.GroupBy(p => p.Prefix, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).ToList();

        duplicates.ShouldBeEmpty(
            $"Route prefixes must be unique: {string.Join("; ", duplicates.Select(d => $"{d.Key} used by {string.Join(", ", d.Select(x => x.Group))}"))}");
    }

    [Fact]
    public void NoMvcControllers()
    {
        var controllers = s_webApiAssembly.GetTypes()
            .Where(t => t.BaseType is not null && t.BaseType.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase")
            .ToList();

        controllers.ShouldBeEmpty(
            $"MVC controllers are not used in this template; implement IEndpointGroup instead: {string.Join(", ", controllers.Select(c => c.Name))}");
    }
}
