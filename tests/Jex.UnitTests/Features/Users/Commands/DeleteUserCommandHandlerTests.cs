using System.Linq.Expressions;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Commands.DeleteUser;
using Jex.Domain.Entities;
using Moq;

namespace Jex.UnitTests.Features.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _handler = new DeleteUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_DeletesUser()
    {
        // Arrange
        var command = new DeleteUserCommand(1);
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), default))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.DeleteAsync(1, default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new DeleteUserCommand(99);
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), default))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<long>(), default), Times.Never);
    }
}
