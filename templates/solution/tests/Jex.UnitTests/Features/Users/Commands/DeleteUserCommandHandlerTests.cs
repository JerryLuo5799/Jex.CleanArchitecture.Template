using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Commands.DeleteUser;

namespace Jex.UnitTests.Features.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _handler = new DeleteUserCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_DeletesThroughIdentityService()
    {
        await _handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        _identityServiceMock.Verify(s => s.DeleteUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_PropagatesNotFoundException()
    {
        _identityServiceMock
            .Setup(s => s.DeleteUserAsync(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User", 99));

        await Should.ThrowAsync<NotFoundException>(() => _handler.Handle(new DeleteUserCommand(99), CancellationToken.None));
    }
}
