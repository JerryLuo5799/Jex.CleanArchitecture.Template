using Jex.Application.Common;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Auth.Commands.Login;

namespace Jex.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        _identityServiceMock
            .Setup(s => s.GetAuthTokenAsync("user@example.com", "Secret123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jwt-token");

        var token = await _handler.Handle(new LoginCommand("user@example.com", "Secret123!"), CancellationToken.None);

        token.ShouldBe("jwt-token");
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsUnauthenticatedWithGenericMessage()
    {
        _identityServiceMock
            .Setup(s => s.GetAuthTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var exception = await Should.ThrowAsync<UnauthenticatedException>(
            () => _handler.Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None));

        exception.Code.ShouldBe(ErrorCodes.Unauthenticated);
        exception.Message.ShouldBe("Invalid email or password.");
    }
}
