using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Domain.Enums;

namespace Jex.UnitTests.Features.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _handler = new UpdateUserCommandHandler(_repositoryMock.Object, _identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesThroughIdentityService()
    {
        var command = new UpdateUserCommand(1, "Jane", "New", "new@example.com", UserStatus.Active);

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(command, CancellationToken.None);

        _identityServiceMock.Verify(
            s => s.UpdateUserAsync(1, "Jane", "New", "new@example.com", UserStatus.Active, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_PropagatesNotFoundException()
    {
        var command = new UpdateUserCommand(99, "Jane", "New", "new@example.com", UserStatus.Active);

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _identityServiceMock
            .Setup(s => s.UpdateUserAsync(99, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserStatus>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User", 99));

        await Should.ThrowAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        var command = new UpdateUserCommand(1, "Jane", "New", "taken@example.com", UserStatus.Active);

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Should.ThrowAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));

        exception.Errors.ShouldContainKey(nameof(command.Email));
        _identityServiceMock.Verify(
            s => s.UpdateUserAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserStatus>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
