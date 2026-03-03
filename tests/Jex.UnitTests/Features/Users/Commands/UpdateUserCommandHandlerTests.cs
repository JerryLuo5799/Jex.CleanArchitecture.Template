using Jex.Application.Common.Exceptions;
using Jex.Application.Common;
using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Moq;

namespace Jex.UnitTests.Features.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _handler = new UpdateUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesUser()
    {
        // Arrange
        var existing = new User { Id = 1, FirstName = "Old", LastName = "Name", Email = "old@example.com", Status = UserStatus.Active };
        var command = new UpdateUserCommand(1, "New", "Name", "new@example.com", UserStatus.Inactive);

        _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.IsEmailUniqueAsync("new@example.com", 1, default)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.FirstName == "New" &&
            u.LastName == "Name" &&
            u.Email == "new@example.com" &&
            u.Status == UserStatus.Inactive), default), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdateUserCommand(99, "X", "Y", "x@example.com", UserStatus.Active);
        _repositoryMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var existing = new User { Id = 1, Email = "original@example.com" };
        var command = new UpdateUserCommand(1, "X", "Y", "taken@example.com", UserStatus.Active);

        _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.IsEmailUniqueAsync("taken@example.com", 1, default)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(nameof(command.Email)));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }
}
