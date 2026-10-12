using Jex.Application.Features.Users.Commands.CreateUser;
using Jex.Application.Features.Users.Commands.DeleteUser;
using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Application.Features.Users.Queries.GetUsers;
using Jex.Domain.Enums;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Endpoints;

/// <summary>User management. Mapped under <c>/api/users</c>; every endpoint requires a JWT.</summary>
public sealed class Users : IEndpointGroup
{
    public static string RoutePrefix => "/users";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        group.MapGet(GetUsers);
        group.MapGet(GetUserById, "{id:long}");
        group.MapPost(CreateUser);
        group.MapPut(UpdateUser, "{id:long}");
        group.MapDelete(DeleteUser, "{id:long}");
    }

    private static async Task<ApiResponse<List<UserDto>>> GetUsers(
        ISender sender, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var users = await sender.Send(new GetUsersQuery(page, pageSize), cancellationToken);
        return ApiResponse.Success(users);
    }

    private static async Task<ApiResponse<UserDto>> GetUserById(ISender sender, long id, CancellationToken cancellationToken)
        => ApiResponse.Success(await sender.Send(new GetUserByIdQuery(id), cancellationToken));

    private static async Task<ApiResponse<CreatedResponse>> CreateUser(ISender sender, CreateUserCommand command, CancellationToken cancellationToken)
        => ApiResponse.Success(new CreatedResponse(await sender.Send(command, cancellationToken)));

    private static async Task<ApiResponse<object>> UpdateUser(ISender sender, long id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.Status), cancellationToken);
        return ApiResponse.Success();
    }

    private static async Task<ApiResponse<object>> DeleteUser(ISender sender, long id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return ApiResponse.Success();
    }
}

/// <summary>Body of <c>PUT /api/users/{id}</c>; the id comes from the route.</summary>
public sealed record UpdateUserRequest(string FirstName, string LastName, string Email, UserStatus Status);
