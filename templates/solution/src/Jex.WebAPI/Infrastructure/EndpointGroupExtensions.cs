using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Discovers every <see cref="IEndpointGroup"/> in the assembly and maps it under <c>/api{RoutePrefix}</c>.
/// Also provides <c>MapGet/MapPost/MapPut/MapPatch/MapDelete</c> overloads that take the handler first and
/// name the endpoint <c>{Group}_{Method}</c>, which gives stable OpenAPI operation ids.
/// </summary>
public static class EndpointGroupExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app, Assembly? assembly = null)
    {
        assembly ??= typeof(EndpointGroupExtensions).Assembly;

        var api = app.MapGroup("/api")
            .AddEndpointFilter<ApiResponseEndpointFilter>();

        var groupTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpointGroup).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal);

        foreach (var type in groupTypes)
        {
            var prefix = (string?)type.GetProperty(nameof(IEndpointGroup.RoutePrefix), BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException($"{type.Name} must declare a static RoutePrefix.");

            var map = type.GetMethod(nameof(IEndpointGroup.Map), BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException($"{type.Name} must declare a static Map(RouteGroupBuilder).");

            var group = api.MapGroup(prefix).WithTags(type.Name);
            map.Invoke(null, [group]);
        }

        return app;
    }

    public static RouteHandlerBuilder MapGet(this RouteGroupBuilder group, Delegate handler, [StringSyntax("Route")] string pattern = "")
        => EndpointRouteBuilderExtensions.MapGet(group, pattern, handler).WithName(EndpointName(handler));

    public static RouteHandlerBuilder MapPost(this RouteGroupBuilder group, Delegate handler, [StringSyntax("Route")] string pattern = "")
        => EndpointRouteBuilderExtensions.MapPost(group, pattern, handler).WithName(EndpointName(handler));

    public static RouteHandlerBuilder MapPut(this RouteGroupBuilder group, Delegate handler, [StringSyntax("Route")] string pattern = "")
        => EndpointRouteBuilderExtensions.MapPut(group, pattern, handler).WithName(EndpointName(handler));

    public static RouteHandlerBuilder MapPatch(this RouteGroupBuilder group, Delegate handler, [StringSyntax("Route")] string pattern = "")
        => EndpointRouteBuilderExtensions.MapPatch(group, pattern, handler).WithName(EndpointName(handler));

    public static RouteHandlerBuilder MapDelete(this RouteGroupBuilder group, Delegate handler, [StringSyntax("Route")] string pattern = "")
        => EndpointRouteBuilderExtensions.MapDelete(group, pattern, handler).WithName(EndpointName(handler));

    /// <summary>Endpoint name <c>{Group}_{Method}</c>, e.g. <c>Users_GetUserById</c>.</summary>
    public static string EndpointName(Delegate handler)
    {
        var method = handler.Method;
        if (method.Name.Contains('<', StringComparison.Ordinal) || method.DeclaringType is null)
        {
            throw new ArgumentException(
                "Endpoint handlers must be named static methods on the endpoint group (not lambdas) so the endpoint " +
                "gets a stable name for OpenAPI and link generation. See AGENTS.md.",
                nameof(handler));
        }

        return $"{method.DeclaringType.Name}_{method.Name}";
    }
}
