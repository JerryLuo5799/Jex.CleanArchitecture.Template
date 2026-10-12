using System.Reflection;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Security;

namespace Jex.Application.Common.Behaviours;

/// <summary>
/// Enforces <see cref="AuthorizeRequestAttribute"/> on the request type:
/// anonymous caller → <see cref="UnauthenticatedException"/> (4010);
/// missing role or failed policy → <see cref="ForbiddenAccessException"/> (4030).
/// Requests without the attribute are not checked here (endpoint-level authorization still applies).
/// </summary>
public sealed class AuthorizationBehaviour<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attributes = request.GetType().GetCustomAttributes<AuthorizeRequestAttribute>().ToList();
        if (attributes.Count == 0)
        {
            return await next(cancellationToken);
        }

        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthenticatedException();
        }

        var roleAttributes = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles)).ToList();
        if (roleAttributes.Count > 0)
        {
            var authorized = roleAttributes
                .SelectMany(a => a.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Any(currentUser.IsInRole);

            if (!authorized)
            {
                throw new ForbiddenAccessException();
            }
        }

        foreach (var policy in attributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy)).Select(a => a.Policy))
        {
            if (!await currentUser.AuthorizeAsync(policy, cancellationToken))
            {
                throw new ForbiddenAccessException();
            }
        }

        return await next(cancellationToken);
    }
}
