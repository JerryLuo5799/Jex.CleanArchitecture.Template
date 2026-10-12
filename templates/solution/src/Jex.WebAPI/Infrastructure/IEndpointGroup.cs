namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// One class per feature under <c>Endpoints/</c>. Implementations are discovered by
/// <see cref="EndpointGroupExtensions.MapEndpoints"/>; nothing has to be registered.
/// <code>
/// public sealed class Orders : IEndpointGroup
/// {
///     public static string RoutePrefix => "/orders";
///
///     public static void Map(RouteGroupBuilder group)
///     {
///         group.RequireAuthorization();
///         group.MapGet(GetOrders);
///         group.MapGet(GetOrderById, "{id:long}");
///         group.MapPost(CreateOrder);
///     }
///
///     private static async Task&lt;ApiResponse&lt;OrderDto&gt;&gt; GetOrderById(ISender sender, long id, CancellationToken ct)
///         => ApiResponse.Success(await sender.Send(new GetOrderByIdQuery(id), ct));
/// }
/// </code>
/// </summary>
public interface IEndpointGroup
{
    /// <summary>Route prefix under <c>/api</c>, e.g. <c>/orders</c>. Declared explicitly so renaming the class never changes URLs.</summary>
    static abstract string RoutePrefix { get; }

    /// <summary>Maps the group's endpoints. Handlers must be named methods (not lambdas).</summary>
    static abstract void Map(RouteGroupBuilder group);
}
