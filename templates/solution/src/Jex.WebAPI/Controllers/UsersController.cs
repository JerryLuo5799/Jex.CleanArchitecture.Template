using Jex.Application.Features.Users.Commands.CreateUser;
using Jex.Application.Features.Users.Commands.DeleteUser;
using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Application.Features.Users.Queries.GetUsers;
using Jex.Domain.Enums;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Controllers;

/// <summary>User management. All endpoints require a valid JWT bearer token.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ApiResponse<List<UserDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUsersQuery(page, pageSize), cancellationToken);
        return ApiResponse.Success(RequestIdentity.Get(HttpContext), result);
    }

    [HttpGet("{id:long}")]
    public async Task<ApiResponse<UserDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return ApiResponse.Success(RequestIdentity.Get(HttpContext), result);
    }

    [HttpPost]
    public async Task<ApiResponse<CreatedResponse>> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return ApiResponse.Success(RequestIdentity.Get(HttpContext), new CreatedResponse(id));
    }

    [HttpPut("{id:long}")]
    public async Task<ApiResponse<object>> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.Status), cancellationToken);
        return ApiResponse.Success<object>(RequestIdentity.Get(HttpContext), null);
    }

    [HttpDelete("{id:long}")]
    public async Task<ApiResponse<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return ApiResponse.Success<object>(RequestIdentity.Get(HttpContext), null);
    }
}

/// <summary>Request body for <see cref="UsersController.Update"/>.</summary>
public sealed record UpdateUserRequest(string FirstName, string LastName, string Email, UserStatus Status);

/// <summary>Payload returned by create endpoints.</summary>
public sealed record CreatedResponse(long Id);
