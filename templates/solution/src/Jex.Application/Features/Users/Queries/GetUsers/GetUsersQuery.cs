using Jex.Application.Common.Messaging;
using Jex.Application.Common.Models;
using Jex.Application.Features.Users.Queries.GetUserById;

namespace Jex.Application.Features.Users.Queries.GetUsers;

/// <summary>Retrieves a page of users.</summary>
public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : PagedQuery(Page, PageSize), IQuery<PagedData<UserDto>>;
