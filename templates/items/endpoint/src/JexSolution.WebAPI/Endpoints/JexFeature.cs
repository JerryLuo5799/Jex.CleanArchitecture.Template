#if (HasEntity)
using JexSolution.Application.Features.JexFeature;
using JexSolution.Application.Features.JexFeature.Commands.CreateJexEntity;
using JexSolution.Application.Features.JexFeature.Queries.GetJexEntityById;
#endif
using JexSolution.WebAPI.Infrastructure;
#if (HasEntity)
using JexSolution.WebAPI.Models;
#endif

namespace JexSolution.WebAPI.Endpoints;

/// <summary>
/// Endpoints for JexFeature, mapped under <c>/api/TPLROUTE</c>. Discovered automatically; nothing to register.
/// Handlers are named static methods (never lambdas) so endpoint names are stable: <c>JexFeature_{Method}</c>.
/// </summary>
public sealed class JexFeature : IEndpointGroup
{
    public static string RoutePrefix => "/TPLROUTE";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();

#if (HasEntity)
        group.MapGet(GetJexEntityById, "{id:long}");
        group.MapPost(CreateJexEntity);
#else
        // TODO: map endpoints, e.g. group.MapGet(GetSomething, "{id:long}"); see Endpoints/TodoItems.cs.
#endif
    }
#if (HasEntity)

    private static async Task<ApiResponse<JexEntityDto>> GetJexEntityById(ISender sender, long id, CancellationToken cancellationToken)
        => ApiResponse.Success(await sender.Send(new GetJexEntityByIdQuery(id), cancellationToken));

    private static async Task<ApiResponse<CreatedResponse>> CreateJexEntity(ISender sender, CreateJexEntityCommand command, CancellationToken cancellationToken)
        => ApiResponse.Success(new CreatedResponse(await sender.Send(command, cancellationToken)));
#endif
}
