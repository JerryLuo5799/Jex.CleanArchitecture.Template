using MediatR;
using Microsoft.AspNetCore.Mvc;
using Jex.Application.Features.Users.Commands.CreateUser;
using Jex.Application.Features.Users.Commands.DeleteUser;
using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Application.Features.Users.Queries.GetUsers;
using Jex.Domain.Enums;

namespace Jex.WebAPI.Controllers;

/// <summary>
/// RESTful API for user management.
/// Each endpoint dispatches a MediatR command or query.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>Gets a paginated list of users.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUsersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a user by Id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Updates an existing user.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.Status);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a user by Id.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for the Update endpoint.</summary>
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    UserStatus Status);
