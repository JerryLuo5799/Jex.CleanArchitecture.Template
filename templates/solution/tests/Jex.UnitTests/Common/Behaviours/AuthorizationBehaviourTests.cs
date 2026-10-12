using Jex.Application.Common.Behaviours;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Security;

namespace Jex.UnitTests.Common.Behaviours;

public class AuthorizationBehaviourTests
{
    private sealed record OpenRequest : IRequest<string>;

    [AuthorizeRequest]
    private sealed record AuthenticatedRequest : IRequest<string>;

    [AuthorizeRequest(Roles = "Administrator, Manager")]
    private sealed record RoleRequest : IRequest<string>;

    [AuthorizeRequest(Policy = "CanPurge")]
    private sealed record PolicyRequest : IRequest<string>;

    private readonly Mock<ICurrentUser> _currentUser = new();

    private static Task<string> Next(CancellationToken _) => Task.FromResult("handled");

    [Fact]
    public async Task Handle_RequestWithoutAttribute_SkipsChecks()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(false);

        var result = await new AuthorizationBehaviour<OpenRequest, string>(_currentUser.Object)
            .Handle(new OpenRequest(), Next, CancellationToken.None);

        result.ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_AnonymousCaller_ThrowsUnauthenticated()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(false);

        await Should.ThrowAsync<UnauthenticatedException>(() =>
            new AuthorizationBehaviour<AuthenticatedRequest, string>(_currentUser.Object)
                .Handle(new AuthenticatedRequest(), Next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingRole_ThrowsForbidden()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            new AuthorizationBehaviour<RoleRequest, string>(_currentUser.Object)
                .Handle(new RoleRequest(), Next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AnyListedRole_Passes()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.IsInRole("Manager")).Returns(true);

        var result = await new AuthorizationBehaviour<RoleRequest, string>(_currentUser.Object)
            .Handle(new RoleRequest(), Next, CancellationToken.None);

        result.ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_FailedPolicy_ThrowsForbidden()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.AuthorizeAsync("CanPurge", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            new AuthorizationBehaviour<PolicyRequest, string>(_currentUser.Object)
                .Handle(new PolicyRequest(), Next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassedPolicy_Continues()
    {
        _currentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.AuthorizeAsync("CanPurge", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new AuthorizationBehaviour<PolicyRequest, string>(_currentUser.Object)
            .Handle(new PolicyRequest(), Next, CancellationToken.None);

        result.ShouldBe("handled");
    }
}
