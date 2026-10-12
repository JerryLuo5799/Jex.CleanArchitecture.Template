namespace Jex.Application.Common.Security;

/// <summary>
/// Declares that a command or query requires an authenticated caller, optionally with roles
/// and/or a policy. Enforced by <see cref="Behaviours.AuthorizationBehaviour{TRequest, TResponse}"/>
/// before validation and the handler run.
/// <code>
/// [AuthorizeRequest(Roles = Roles.Administrator)]
/// public sealed record DeleteUserCommand(long Id) : IRequest;
/// </code>
/// Multiple attributes may be stacked: roles are OR-ed within one attribute (comma separated),
/// every policy must pass.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AuthorizeRequestAttribute : Attribute
{
    /// <summary>Comma separated list of roles; the caller needs at least one of them.</summary>
    public string Roles { get; set; } = string.Empty;

    /// <summary>Policy name that must be satisfied.</summary>
    public string Policy { get; set; } = string.Empty;
}
